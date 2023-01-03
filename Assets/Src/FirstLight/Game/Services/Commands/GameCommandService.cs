using System;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Services;
using PlayFab;

namespace FirstLight.Game.Services
{


	/// <inheritdoc cref="ICommandService{TGameLogic}"/>
	public interface IGameCommandService
	{
		/// <inheritdoc cref="ICommandService{TGameLogic}.ExecuteCommand{TCommand}"/>
		void ExecuteCommand<TCommand>(TCommand command) where TCommand : IGameCommand;
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