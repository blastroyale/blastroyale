using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
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
	
		public static readonly string CommandFieldName = nameof(IGameCommand);
		
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
				ExecuteCommandServerSide(command); 
				command.Execute(_gameLogic, _dataProvider);
				// ForceServerDataUpdate(command);
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
			var descriptiveError = $"{error.ErrorMessage}: {JsonConvert.SerializeObject(error.ErrorDetails)}";
			throw new PlayFabException(PlayFabExceptionCode.AuthContextRequired, descriptiveError);
		}
		
		/// <summary>
		/// Sends a command to override playfab data with what the client is sending.
		/// Will only be enabled for testing and debugging purposes.
		/// </summary>
		public void ForceServerDataUpdate()
		{
			var data = new Dictionary<string, string>();
			AddSerializedModels(data, 
				_dataProvider.GetData<IdData>(),
				_dataProvider.GetData<RngData>(),
				_dataProvider.GetData<PlayerData>());
			ExecuteServerCommand(null, data);
		}

		/// <summary>
		/// Serializes a given command and sends it to the server. The server should run the command logic server-side.
		/// If no errors are provided in return, assume the server and client are in sync.
		/// This will also always send the Rng data to the server in case there's any rng rolls.
		/// TODO: In case of error, rollback client state.
		/// </summary>
		private void ExecuteCommandServerSide(IGameCommand command)
		{
			var data = new Dictionary<string, string>
			{
				{CommandFieldName, ModelSerializer.Serialize(command).Value},
			};
			ExecuteServerCommand(command, data);
		}

		/// <summary>
		/// Add serialized models to the data dictionary.
		/// Keys will be a string repr. of the model type name
		/// Values will be the serialized model data.
		/// </summary>
		private void AddSerializedModels(Dictionary<string, string> dict, params object [] models)
		{
			foreach (var model in models)
			{
				var (modelKey, modelValue) = ModelSerializer.Serialize(model);
				dict[modelKey] = modelValue;
			}
		}
		
		/// <summary>
		/// Sends a command to the server.
		/// </summary>
		private void ExecuteServerCommand(IGameCommand command, Dictionary<string, string> data) 
		{
			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = command?.GetType().FullName,
					Platform = Application.platform.ToString(),
					Data = data
				},
				AuthenticationContext = PlayFabSettings.staticPlayer
			};
			PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
		}
	}
}