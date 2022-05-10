using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Services;
using Newtonsoft.Json;
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
		Player, Admin
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
		private readonly Queue<IGameCommand> _commandQueue;
		
		public GameCommandService(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			_gameLogic = gameLogic;
			_dataProvider = dataProvider;
			_commandQueue = new Queue<IGameCommand>();
		}
		
		/// <summary>
		/// Generic PlayFab error that is being called on PlayFab responses.
		/// Will throw an <see cref="PlayFabException"/> to be shown to the player.
		/// </summary>
		public static void OnPlayFabError(PlayFabError error)
		{
			var descriptiveError = $"{error.ErrorMessage}: {JsonConvert.SerializeObject(error.ErrorDetails)}";
			throw new PlayFabException(PlayFabExceptionCode.AuthContextRequired, descriptiveError);
		}

		/// <inheritdoc cref="CommandService{TGameLogic}.ExecuteCommand{TCommand}" />
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand
		{
			try
			{
				EnqueueCommandToServer(command);
				command.Execute(_gameLogic, _dataProvider);
			}
			catch (Exception e)
			{
				var title = "Game Exception";
				var button = new AlertButton
				{
					Callback = Application.Quit,
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
			ExecuteServerCommand(new ForceUpdateCommand()
			{
				IdData = _dataProvider.GetData<IdData>(),
				RngData = _dataProvider.GetData<RngData>(),
				PlayerData = _dataProvider.GetData<PlayerData>()
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
		/// Rolls back client state to the current server state.
		/// Current implementation it simply closes the game.
		/// </summary>
		private void Rollback()
		{
			var button = new AlertButton
			{
				Callback = Application.Quit,
				Style = AlertButtonStyle.Negative,
				Text = "Quit Game"
			};
			NativeUiService.ShowAlertPopUp(false, "Game Error", "Server Desync", button);
		}

		/// <summary>
		/// Sends a command to the server.
		/// </summary>
		private void ExecuteServerCommand<TCommand>(TCommand command) where TCommand : IGameCommand
		{
			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = command.GetType().FullName,
					Platform = Application.platform.ToString(),
					Data = new Dictionary<string, string>
					{
						{ CommandFields.Command, ModelSerializer.Serialize(command).Value},
						{ CommandFields.Timestamp, DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
						{ CommandFields.ClientVersion, VersionUtils.VersionExternal }
					}
				},
				AuthenticationContext = PlayFabSettings.staticPlayer
			};
			PlayFabCloudScriptAPI.ExecuteFunction(request, OnCommandSuccess, OnCommandError);
		}
		
		/// <summary>
		/// Whenever the HTTP request to proccess a command does not return 200
		/// </summary>
		private void OnCommandError(PlayFabError error)
		{
			Rollback();
			OnPlayFabError(error);
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
				Rollback();
				throw new LogicException($"Queue waiting for {current.GetType().FullName} command but {logicResult.Result.Command} was received");
			}
			OnServerExecutionFinished(current);
		}
	}
}