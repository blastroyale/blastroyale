using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.NativeUi;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using PlayFab.CloudScriptModels;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <inheritdoc cref="ICommandService{TGameLogic}"/>
	public interface IGameCommandService
	{
		/// <inheritdoc cref="ICommandService{TGameLogic}.ExecuteCommand{TCommand}"/>
		void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand;
	}

	/// <inheritdoc />
	public class GameCommandService : IGameCommandService
	{
		private readonly IDataProvider _dataProvider;
		private readonly IGameLogic _gameLogic;
		private readonly JsonConverter _formatter;
		
		public GameCommandService(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			_gameLogic = gameLogic;
			_dataProvider = dataProvider;
			_formatter = new StringEnumConverter();
		}
		
		/// <inheritdoc cref="CommandService{TGameLogic}.ExecuteCommand{TCommand}" />
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand
		{
			try
			{
				// ExecuteCommandServerSide(command); // TODO: Add me
				command.Execute(_gameLogic, _dataProvider);
				ForceServerDataUpdate(command); // TODO: Remove me
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
		/// Generic PlayFab error that is being called on PlayFab responses.
		/// Will throw an <see cref="PlayFabException"/> to be shown to the player.
		/// </summary>
		public static void OnPlayFabError(PlayFabError error)
		{
			throw new PlayFabException(PlayFabExceptionCode.AuthContextRequired, error.ErrorMessage);
		}

		/// <summary>
		/// Serializes a given command and sends it to the server. The server should run the command logic server-side.
		/// If no errors are provided in return, assume the server and client are in sync.
		/// This will also always send the Rng data to the server in case there's any rng rolls.
		/// TODO: In case of error, rollback client state.
		/// </summary>
		/// <param name="command"></param>
		private void ExecuteCommandServerSide(IGameCommand command)
		{
			var data = new Dictionary<string, string>
			{
				{nameof(IGameCommand), SerializeCommandToServer(command)},
			};
			ExecuteServerCommand(command, data);
		}

		/// <summary>
		/// Serializes the command as a Dictionary.
		/// </summary>
		/// <param name="command">Command to be serialized</param>
		/// <returns>A dictionary with the serialized command</returns>
		public string SerializeCommandToServer(IGameCommand command)
		{
			return JsonConvert.SerializeObject(command, _formatter);
		}
		
		/// <summary>
		/// Sends a command to override playfab data with what the client is sending.
		/// Will only be enabled for testing and debugging purposes.
		/// </summary>
		/// <param name="command">Command to be sent</param>
		private void ForceServerDataUpdate(IGameCommand command)
		{
			var data = new Dictionary<string, string>
			{
				{nameof(IdData), JsonConvert.SerializeObject(_dataProvider.GetData<IdData>(), _formatter)},
				{nameof(RngData), JsonConvert.SerializeObject(_dataProvider.GetData<RngData>(), _formatter)},
				{nameof(PlayerData), JsonConvert.SerializeObject(_dataProvider.GetData<PlayerData>(), _formatter)}
			};
			ExecuteServerCommand(command, data);
		}
		
		/// <summary>
		/// Sends a command to the server.
		/// </summary>
		/// <param name="command">Command to be ran in server</param>
		/// <param name="data">Command parameters to be sent</param>
		private void ExecuteServerCommand(IGameCommand command, Dictionary<string, string> data) 
		{
			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = command.GetType().Name,
					Platform = Application.platform.ToString(),
					Data = data
				},
				AuthenticationContext = PlayFabSettings.staticPlayer
			};
			PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
		}
	}
}