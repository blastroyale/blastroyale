using System;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using PlayFab;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Defines the required user permission level to access a given command.
	/// TODO: Move to server SDK
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
	/// TODO: Move to server SDK
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
		private readonly IGameServices _services;
		private readonly ServerCommandQueue _serverCommandQueue;
		private readonly CommandExecutionContext _commandContext;

		public GameCommandService(IPlayfabService playfabService, IGameLogic gameLogic, IDataService dataService,
								  IGameServices services)
		{
			_dataService = dataService;
			_services = services;
			_serverCommandQueue = new ServerCommandQueue(dataService, gameLogic, playfabService, services);
			_commandContext = new CommandExecutionContext(
														  new LogicContainer().Build(gameLogic), new ServiceContainer().Build(services), dataService);
		}


		/// <inheritdoc cref="CommandService{TGameLogic}.ExecuteCommand{TCommand}" />
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : IGameCommand
		{
#if UNITY_EDITOR
			// Ensure we go trough serialization & deserialization process Editor
			var serializedCommand = ModelSerializer.Serialize(command).Value;
			command = (TCommand)ModelSerializer.Deserialize(command.GetType(), serializedCommand);
#endif
			try
			{
				command.Execute(_commandContext);
				
				switch (command.ExecutionMode())
				{
					case CommandExecutionMode.Quantum:
						if (!FeatureFlags.QUANTUM_CUSTOM_SERVER)
						{
							_serverCommandQueue.EnqueueCommand(command);
						}

						break;
					case CommandExecutionMode.Server:
						_serverCommandQueue.EnqueueCommand(command);
						break;
				}
				
			}
			catch (Exception e)
			{
				var title = "Game Exception";
				var button = new AlertButton
				{
					Callback = () => { _services.QuitGame("Closing game exception popup"); },
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
			var data = _dataService.GetKeys().ToDictionary(type => type, type => _dataService.GetData(type));
			_serverCommandQueue.EnqueueCommand(new ForceUpdateCommand(data: data));
		}
	}
}