using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.SharedModels;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// A queue implementation to dispatch server commands checking Data Synchronization between server and client
	/// </summary>
	public class ServerCommandQueue
	{
		private IDataService _data;
		private IGameLogic _logic;
		private IGameLogicInitializer _logicInitializer;
		private IGameBackendService _gameBackend;
		private Queue<ServerCommandQueueEntry> _queue;
		private IGameServices _services;
		
		public ServerCommandQueue(IDataService data, IGameLogic logic, IGameBackendService gameBackend, IGameServices services)
		{
			_data = data;
			_logic = logic;
			_logicInitializer = logic as IGameLogicInitializer;
			_gameBackend = gameBackend;
			_queue = new();
			_services = services;
		}
		
		/// <summary>
		/// Get a StateDelta object with all the hashes from the data stored in <paramref name="data"/>
		/// TODO: Move to server when IDataProvider moves to server
		/// </summary>
		public static StateDelta GetClientDelta(IDataProvider data)
		{
			var delta = new StateDelta();
			foreach (var type in data.GetKeys())
			{
				if (data.TryGetData(type, out var currentData))
				{
					delta.TrackModification(currentData);
				}
			}

			return delta;
		}

		/// <summary>
		/// By a given server response, tries to identifies any delta missmatch to detect
		/// data desynch betwen client & server
		/// TODO: Move to server when IDataProvider moves to server
		/// </summary>
		public static List<Type> GetDesynchedDeltas(StateDelta clientDelta, Dictionary<string, string> serverResponse)
		{
			var delta = ModelSerializer.DeserializeFromData<StateDelta>(serverResponse, true);
			var invalid = new List<Type>();
			if (!delta.ModifiedTypes.Any())
				return invalid;

			foreach (var modifiedType in delta.ModifiedTypes.Keys)
			{
				if (clientDelta.ModifiedTypes.TryGetValue(modifiedType, out var modified) && modified != delta.ModifiedTypes[modifiedType])
				{
					invalid.Add(modifiedType);
				}
			}

			return invalid;
		}
		

		/// <summary>
		/// Adds a given command to the "to send to server queue".
		/// We send one command at a time to server, this queue ensure that.
		/// </summary>
		public void EnqueueCommand(IGameCommand command)
		{
			if (FeatureFlags.GetLocalConfiguration().OfflineMode)
			{
				return;
			}
			var entry = new ServerCommandQueueEntry(GetClientDelta(_data), command);
			_queue.Enqueue(entry);
			if (_queue.Count == 1)
			{
				RunNext();
			}
		}


		/// <summary>
		/// Whenever the HTTP request to proccess a command returns 200
		/// </summary>
		private void OnCommandSuccess(ExecuteFunctionResult result)
		{
			_queue.TryPeek(out var current);
			var logicResult = JsonConvert.DeserializeObject<PlayFabResult<LogicResult>>(result.FunctionResult.ToString());
			if (logicResult.Result.Command != current.GameCommand.GetType().FullName)
			{
				throw new LogicException($"Queue waiting for {current.GameCommand.GetType().FullName} command but {logicResult.Result.Command} was received");
			}

			// Command returned 200 but a expected logic exception happened due
			if (logicResult.Result.Data.TryGetValue("LogicException", out var logicException))
			{
				OnCommandException(logicException);
			}

			var desynchs = GetDesynchedDeltas(current.ClientDelta, logicResult.Result.Data);
			
			if (FeatureFlags.REMOTE_CONFIGURATION &&
			    logicResult.Result.Data.TryGetValue(CommandFields.ConfigurationVersion, out var serverConfigVersion))
			{
				var serverVersionNumber = ulong.Parse(serverConfigVersion);
				if (serverVersionNumber > _logic.ConfigsProvider.Version)
				{
					FLog.Verbose("Client configs outdated, updating !");
					UpdateConfiguration(serverVersionNumber, current);
					if (desynchs.Count > 0)
					{
						RollbackToServerState(current.GameCommand, desynchs);
						return;
					}
				}
			}

			if (FeatureFlags.DESYNC_DETECTION)
			{
				if (desynchs.Count > 0)
				{
#if !DISABLE_SRDEBUGGER && !UNITY_EDITOR
					SROptions.Current.SendQuietBugReport($"models desynched {string.Join(',', desynchs)}");
#endif
					
					OnCommandException($"Models desynched: {string.Join(',', desynchs)}");
					// TODO: Do a json diff and show which data exactly is different
				}
			}
			OnServerExecutionFinished();
		}

		/// <summary>
		/// Whenever the HTTP request to proccess a command does not return 200
		/// </summary>
		private void OnCommandError(PlayFabError error)
		{
#if UNITY_EDITOR
			_queue.Clear(); // clear to make easier for testing
#endif
		}


		/// <summary>
		/// When server returns an exception after a command was executed
		/// </summary>
		private void OnCommandException(string exceptionMsg)
		{
			FLog.Info($"Command exception: {exceptionMsg}");

			DebugUtils.SaveState(_gameBackend, _data, () =>
			{
#if UNITY_EDITOR
				FLog.Error(exceptionMsg);
				var confirmButton = new GenericDialogButton
				{
					ButtonText = "OK",
					ButtonOnClick = () =>
					{
						_services.AnalyticsService.CrashLog(exceptionMsg);
						_services.QuitGame(exceptionMsg);
					}
				};
				_services.GenericDialogService.OpenButtonDialog("Server Error", exceptionMsg, false, confirmButton);
#else
			NativeUiService.ShowAlertPopUp(false, "Error", "Desynch", new AlertButton
			{
				Callback = () =>
				{
					_services.AnalyticsService.CrashLog(exceptionMsg);
					_services.QuitGame("Server desynch");
				},
				Style = AlertButtonStyle.Negative,
				Text = "Quit Game"
			});
#endif
			});
		}

		private void UpdateConfiguration(ulong serverVersion, ServerCommandQueueEntry lastCommand)
		{
			var configAdder = _logic.ConfigsProvider as IConfigsAdder;
			_gameBackend.GetTitleData(PlayfabConfigKeys.ConfigName, configString =>
			{
				var updatedConfig = new ConfigsSerializer().Deserialize<ConfigsProvider>(configString);
				configAdder.UpdateTo(serverVersion, updatedConfig.GetAllConfigs());
				FLog.Info($"Updated game configs to version {serverVersion}");
			}, null);
		}

		/// <summary>
		/// Fetches current server state and override client's state.
		/// Will not cause a UI refresh.
		/// </summary>
		private void RollbackToServerState<TCommand>(TCommand lastCommand, List<Type> desynced) where TCommand : IGameCommand
		{
			FLog.Warn("Desync caused by config updates - rolling back last command");
			bool rolledBack = false;
			_gameBackend.FetchServerState(state =>
			{
				foreach (var typeFullName in state.Keys)
				{
					var type = Assembly.GetExecutingAssembly().GetType(typeFullName);
					if(desynced.Contains(type))
					{
						rolledBack = true;
						_data.AddData(type, ModelSerializer.DeserializeFromData(type, state));
					}
				}
				FLog.Verbose("Fetched user state from server");
				if (rolledBack)
				{
					FLog.Info("Rolling back client UI");
					_logicInitializer.ReInit();
				}
				OnServerExecutionFinished();
			}, null);
		}

		/// <summary>
		/// Called when server has successfully finished running the given command.
		/// </summary>
		private void OnServerExecutionFinished()
		{
			_queue.Dequeue();
			RunNext();
		}

		private void RunNext()
		{
			if (_queue.Count == 0)
			{
				return;
			}

			if (_queue.TryPeek(out var next))
			{
				ExecuteServerCommand(next.GameCommand);
			}
		}

		/// <summary>
		/// Sends a command to the server.
		/// </summary>
		private void ExecuteServerCommand(IGameCommand command)
		{
			FLog.Verbose($"Sending server command {command.GetType().Name}");
			var request = new LogicRequest
			{
				Command = command.GetType().FullName,
				Platform = Application.platform.ToString(),
				Data = new Dictionary<string, string>
				{
					{ CommandFields.Command, ModelSerializer.Serialize(command).Value },
					{ CommandFields.Timestamp, DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
					{ CommandFields.ClientVersion, VersionUtils.VersionExternal },
					{ CommandFields.ConfigurationVersion, _logic.ConfigsProvider.Version.ToString() }
				}
			};
			_gameBackend.CallFunction("ExecuteCommand", OnCommandSuccess, OnCommandError, request);
		}
		

		private class ServerCommandQueueEntry
		{
			/// <summary>
			/// Snapshot of client delta after executing this command locally
			/// </summary>
			public StateDelta ClientDelta { get; }
			/// <summary>
			/// Command to execute in the server
			/// </summary>
			public IGameCommand GameCommand { get; }


			public ServerCommandQueueEntry(StateDelta clientDelta, IGameCommand gameCommand)
			{
				ClientDelta = clientDelta;
				GameCommand = gameCommand;
			}
		}
		
	}
}