using System;
using System.Linq;
using FirstLight.FLogger;
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

		/// <summary>
		/// Executes a command and returns the result of it, only works with IGameCommandWithResult implementations
		/// </summary>
		T ExecuteCommandWithResult<T>(IGameCommandWithResult<T> command);
	}

	/// <inheritdoc />
	public class GameCommandService : IGameCommandService
	{
		private readonly IDataService _dataService;
		private readonly IGameServices _services;
		private readonly IGameLogic _logic;
		private readonly ServerCommandQueue _serverCommandQueue;
		private CommandExecutionContext _commandContext;

		public GameCommandService(IGameBackendService gameBackendService, IGameLogic gameLogic, IDataService dataService,
								  IGameServices services)
		{
			_logic = gameLogic;
			_dataService = dataService;
			_services = services;
			_serverCommandQueue = new ServerCommandQueue(dataService, gameLogic, gameBackendService, services);
		}

		private CommandExecutionContext GetContext()
		{
			if (_commandContext == null)
			{
				_commandContext = new CommandExecutionContext(
					new LogicContainer().Build(_logic), new ServiceContainer().Build(_services), _dataService);
			}

			return _commandContext;
		}

		/// <inheritdoc cref="CommandService{TGameLogic}.ExecuteCommand{TCommand}" />
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : IGameCommand
		{
#if UNITY_EDITOR
			// Ensure we go trough serialization & deserialization process Editor
			var serializedCommand = ModelSerializer.Serialize(command).Value;
			var tempCmd = (TCommand) ModelSerializer.Deserialize(command.GetType(), serializedCommand);
			if (!command.GetType().IsImplementationOf(typeof(IGameCommandWithResult<>)))
			{
				command = tempCmd;
			}
#endif
			try
			{
				command.Execute(GetContext()).GetAwaiter().GetResult();
				switch (command.ExecutionMode())
				{
					case CommandExecutionMode.Quantum:
						if (!_services.GameBackendService.RunsSimulationOnServer())
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

		public T ExecuteCommandWithResult<T>(IGameCommandWithResult<T> command)
		{
			ExecuteCommand(command);
			return command.GetResult();
		}

		/// <summary>
		/// When server returns an exception after a command was executed
		/// </summary>
		private void OnCommandException(string exceptionMsg)
		{
#if UNITY_EDITOR
			FLog.Error(exceptionMsg);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = "OK",
				ButtonOnClick = () =>
				{
					_services.QuitGame(exceptionMsg);
				}
			};
			_services.GenericDialogService.OpenButtonDialog("Server Error", exceptionMsg, false, confirmButton);
#else
			NativeUiService.ShowAlertPopUp(false, "Error", "Desynch", new AlertButton
			{
				Callback = () =>
				{
					_services.QuitGame("Server desynch");
				},
				Style = AlertButtonStyle.Negative,
				Text = "Quit Game"	
			});
#endif
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