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
		void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand;
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
	}

	/// <inheritdoc />
	public class GameCommandService : IGameCommandService
	{
		private readonly IDataProvider _dataProvider;
		private readonly IGameLogic _gameLogic;
		private readonly IGameServices _services;
		private readonly Queue<IGameCommand> _commandQueue;
		private readonly IPlayfabService _playfab;
		private readonly IGameNetworkService _network;
		
		public GameCommandService(IPlayfabService playfabService, IGameLogic gameLogic, IDataProvider dataProvider,
		                          IGameServices services, IGameNetworkService network)
		{
			_playfab = playfabService;
			_gameLogic = gameLogic;
			_dataProvider = dataProvider;
			_services = services;
			_commandQueue = new Queue<IGameCommand>();
			_network = network;
			ModelSerializer.RegisterConverter(new QuantumVector2Converter());
			ModelSerializer.RegisterConverter(new QuantumVector3Converter());
		}

		/// <summary>
		/// Sends the command to quantum
		/// </summary>
		private void ExecuteConsensusCommand<TCommand>(TCommand command) where TCommand : IGameCommand
		{
			FLog.Verbose($"Sending quantum consensus command {command.GetType().Name}");
			var json = ModelSerializer.Serialize(command);
			var opt = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.All
			};
			_network.QuantumClient.OpRaiseEvent(
				(int)QuantumCustomEvents.ConsensusCommand, Encoding.UTF8.GetBytes($"{json.Key}:{json.Value}"), opt, SendOptions.SendReliable
			);
		}

		/// <inheritdoc cref="CommandService{TGameLogic}.ExecuteCommand{TCommand}" />
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand
		{
			try
			{
				switch (command.CommandExecutionMode)
				{
					case CommandExecutionMode.QuantumConsensus:
						if(FeatureFlags.QUANTUM_CUSTOM_SERVER)
						{
							ExecuteConsensusCommand(command);
						} else
						{
							EnqueueCommandToServer(command);
						}
						break;
					case CommandExecutionMode.Server:
						EnqueueCommandToServer(command);
						break;
				}
				command.Execute(_gameLogic, _dataProvider);
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
				IdData = _dataProvider.GetData<IdData>(),
				RngData = _dataProvider.GetData<RngData>(),
				PlayerData = _dataProvider.GetData<PlayerData>(),
				EquipmentData = _dataProvider.GetData<EquipmentData>()
			});
		}

		/// <summary>
		/// Adds a given command to the "to send to server queue".
		/// We send one command at a time to server, this queue ensure that.
		/// </summary>
		private void EnqueueCommandToServer<TCommand>(TCommand cmd) where TCommand : struct, IGameCommand
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
					{CommandFields.ClientVersion, VersionUtils.VersionExternal}
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
			_commandQueue.Clear(); 	// clear to make easier for testing
#endif
			_playfab.HandleError(error);
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
			OnServerExecutionFinished(current);
		}
	}
}