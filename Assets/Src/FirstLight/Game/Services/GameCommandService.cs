using System;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Newtonsoft.Json;
using Photon.Realtime;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.SharedModels;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Defines the required user permission level to access a given command.
	/// </summary>
	public enum CommandAccessLevel
	{
		/// <summary>
		/// Standard permission, allows command to be ran only for the given authenticated player.
		/// </summary>
		Player,

		/// <summary>
		/// Only allows the command to be ran for the given authenticated player but Admin commands might
		/// perform operations normal players can't like cheats.
		/// </summary>
		Admin,

		/// <summary>
		/// Service commands might be used for any given player without requiring player authentication.
		/// It will impersonate a player to run the command from a third party service.
		/// Will require a secret key to run the command.
		/// </summary>
		Service
	}

	/// <inheritdoc cref="ICommandService{TGameLogic}"/>
	public interface IGameCommandService
	{
		/// <inheritdoc cref="ICommandService{TGameLogic}.ExecuteCommand{TCommand}"/>
		void ExecuteCommand<TCommand>(TCommand command) where TCommand : IGameCommand;
	}

	/// <summary>
	/// Refers to dictionary keys used in the data sent to server.
	/// </summary>
	public static class CommandFields
	{
		/// <summary>
		/// Key where the command data is serialized.
		/// </summary>
		public static readonly string Command = nameof(IGameCommand);

		/// <summary>
		/// Field containing the client timestamp for when the command was issued.
		/// </summary>
		public static readonly string Timestamp = nameof(Timestamp);

		/// <summary>
		/// Field about the version the game client is currently running
		/// </summary>
		public static readonly string ClientVersion = nameof(ClientVersion);

		/// <summary>
		/// Field that represents the client configuration version
		/// </summary>
		public static readonly string ConfigurationVersion = nameof(ConfigurationVersion);
	}

	/// <inheritdoc />
	public class GameCommandService : IGameCommandService
	{
		private readonly IDataService _dataService;
		private readonly IGameLogic _gameLogic;
		private readonly IGameServices _services;
		private readonly Queue<IGameCommand> _commandQueue;
		private readonly IPlayfabService _playfab;
		private readonly IGameNetworkService _network;

		public GameCommandService(IPlayfabService playfabService, IGameLogic gameLogic, IDataService dataService,
								  IGameServices services, IGameNetworkService network)
		{
			_playfab = playfabService;
			_gameLogic = gameLogic;
			_dataService = dataService;
			_services = services;
			_commandQueue = new Queue<IGameCommand>();
			_network = network;
			ModelSerializer.RegisterConverter(new QuantumVector2Converter());
			ModelSerializer.RegisterConverter(new QuantumVector3Converter());
		}

		/// <summary>
		/// Sends the command to quantum. Quantum will enrich the command data from the simulation server-side.
		/// All commands will be ran at the end of the match using the last frame.
		/// </summary>
		private void ExecuteQuantumCommand<TCommand>(TCommand command) where TCommand : IGameCommand
		{
			var quantumCommand = command as IQuantumCommand;
			if (quantumCommand == null)
			{
				throw new Exception($"Trying to send {command.GetType().Name} to quantum but that command is not IQuantumCommand");
			}
			FLog.Verbose($"Sending quantum command {command.GetType().Name}");
			var payload = new QuantumCommandPayload()
			{
				CommandType = command.GetType().FullName,
				Token = PlayFabSettings.staticPlayer.EntityToken
			};
			var bytes = Encoding.UTF8.GetBytes(ModelSerializer.Serialize(payload).Value);
			var opt = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.All
			};
			_network.QuantumClient.OpRaiseEvent(
				(int)QuantumCustomEvents.EndGameCommand, bytes, opt, SendOptions.SendReliable
			);
		}

		/// <summary>
		/// Fetches current server state and override client's state.
		/// Will not cause a UI refresh.
		/// </summary>
		private void RollbackToServerState<TCommand>(TCommand lastCommand) where TCommand : IGameCommand
		{
			_playfab.FetchServerState(state =>
			{
				_dataService.AddData(state.DeserializeModel<PlayerData>());
				_dataService.AddData(state.DeserializeModel<RngData>());
				_dataService.AddData(state.DeserializeModel<EquipmentData>());
				_dataService.AddData(state.DeserializeModel<IdData>());
				FLog.Verbose("Fetched user state from server");
				OnServerExecutionFinished(lastCommand);
			});
		}

		/// <inheritdoc cref="CommandService{TGameLogic}.ExecuteCommand{TCommand}" />
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : IGameCommand
		{
			try
			{
				switch (command.ExecutionMode())
				{
					case CommandExecutionMode.Quantum:
						if (FeatureFlags.QUANTUM_CUSTOM_SERVER)
						{
							ExecuteQuantumCommand(command);
						}
						else
						{
							EnqueueCommandToServer(command);
						}
						break;
					case CommandExecutionMode.Server:
						EnqueueCommandToServer(command);
						break;
				}
				command.Execute(_gameLogic, _dataService);
			}
			catch (Exception e)
			{
				var title = "Game Exception";
				var button = new AlertButton
				{
					Callback = () =>
					{
						_services.QuitGame("Closing game exception popup");
					},
					Style = AlertButtonStyle.Negative,
					Text = "Quit Game"
				};

				if (e is LogicException)
				{
					title = "Logic Exception";
				}
				else if (e is PlayFabException)
				{
					title = "PlayFab Exception";
				}
				NativeUiService.ShowAlertPopUp(false, title, e.Message, button);
				throw;
			}
		}

		/// <summary>
		/// Sends a command to override playfab data with what the client is sending.
		/// Will only be enabled for testing and debugging purposes.
		/// </summary>
		public void ForceServerDataUpdate()
		{
			EnqueueCommandToServer(new ForceUpdateCommand()
			{
				IdData = _dataService.GetData<IdData>(),
				RngData = _dataService.GetData<RngData>(),
				PlayerData = _dataService.GetData<PlayerData>(),
				EquipmentData = _dataService.GetData<EquipmentData>()
			});
		}

		/// <summary>
		/// Adds a given command to the "to send to server queue".
		/// We send one command at a time to server, this queue ensure that.
		/// </summary>
		private void EnqueueCommandToServer<TCommand>(TCommand cmd) where TCommand : IGameCommand
		{
			_commandQueue.Enqueue(cmd);
			if (_commandQueue.Count == 1)
			{
				ExecuteServerCommand(cmd);
			}
		}

		/// <summary>
		/// Called when server has successfully finished running the given command.
		/// </summary>
		private void OnServerExecutionFinished<TCommand>(TCommand cmd)
		{
			_commandQueue.Dequeue();
			if (_commandQueue.TryPeek(out var next))
			{
				ExecuteServerCommand(next);
			}
		}

		/// <summary>
		/// Sends a command to the server.
		/// </summary>
		private void ExecuteServerCommand<TCommand>(TCommand command) where TCommand : IGameCommand
		{
			FLog.Verbose($"Sending server command {command.GetType().Name}");
			var request = new LogicRequest
			{
				Command = command.GetType().FullName,
				Platform = Application.platform.ToString(),
				Data = new Dictionary<string, string>
				{
					{CommandFields.Command, ModelSerializer.Serialize(command).Value},
					{CommandFields.Timestamp, DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()},
					{CommandFields.ClientVersion, VersionUtils.VersionExternal},
					{CommandFields.ConfigurationVersion, _gameLogic.ConfigsProvider.Version.ToString()}
				}
			};
			_playfab.CallFunction("ExecuteCommand", OnCommandSuccess, OnCommandError, request);
		}

		/// <summary>
		/// Whenever the HTTP request to proccess a command does not return 200
		/// </summary>
		private void OnCommandError(PlayFabError error)
		{
#if UNITY_EDITOR
			_commandQueue.Clear();  // clear to make easier for testing
#endif
			_playfab.HandleError(error);
		}

		private void UpdateConfiguration(ulong serverVersion, IGameCommand lastCommand)
		{
			var configAdder = _gameLogic.ConfigsProvider as IConfigsAdder;
			_playfab.GetTitleData(PlayfabConfigurationProvider.ConfigName, configString =>
			{
				var updatedConfig = new ConfigsSerializer().Deserialize<ConfigsProvider>(configString);
				configAdder.UpdateTo(serverVersion, updatedConfig.GetAllConfigs());
				FLog.Info($"Updated game configs to version {serverVersion}");
				RollbackToServerState(lastCommand);
			});
		}

		/// <summary>
		/// Whenever the HTTP request to proccess a command returns 200
		/// </summary>
		private void OnCommandSuccess(ExecuteFunctionResult result)
		{
			_commandQueue.TryPeek(out var current);
			var logicResult = JsonConvert.DeserializeObject<PlayFabResult<LogicResult>>(result.FunctionResult.ToString());
			if (logicResult.Result.Command != current.GetType().FullName)
			{
				throw new LogicException($"Queue waiting for {current.GetType().FullName} command but {logicResult.Result.Command} was received");
			}
			// Command returned 200 but a expected logic exception happened due
			if (logicResult.Result.Data.TryGetValue("LogicException", out var logicException))
			{
				throw new LogicException(logicException);
			}
			if (FeatureFlags.REMOTE_CONFIGURATION &&
				logicResult.Result.Data.TryGetValue(CommandFields.ConfigurationVersion, out var serverConfigVersion))
			{
				var serverVersionNumber = ulong.Parse(serverConfigVersion);
				if (serverVersionNumber > _gameLogic.ConfigsProvider.Version)
				{
					FLog.Verbose("Client configs outdated, updating !");
					UpdateConfiguration(serverVersionNumber, current);
					return;
				}
			}
			OnServerExecutionFinished(current);
		}
	}
}