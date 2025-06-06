using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using FirstLightServerSDK.Services;
using I2.Loc;
using Quantum.Systems.Bots;
using Unity.Services.UserReporting;
using Unity.Services.UserReporting.Client;

namespace FirstLight.Game.StateMachines
{
	public interface IGameStateMachine
	{
		string GetCurrentStateDebug();

		void Run();
	}

	/// <summary>
	/// The State Machine that controls the entire flow of the game
	/// </summary>
	public class GameStateMachine : IGameStateMachine
	{
		private readonly Statechart.Statechart _statechart;
		private readonly AuthenticationState _authenticationState;
		private readonly AudioState _audioState;
		private readonly NetworkState _networkState;
		private readonly TutorialState _tutorialState;
		private readonly GameLogic _gameLogic;
		private readonly CoreLoopState _coreLoopState;
		private readonly ReconnectionState _reconnection;
		private readonly IGameServices _services;

		public GameStateMachine(GameLogic gameLogic, IGameServices services,
								IInternalGameNetworkService networkService,
								IAssetAdderService assetAdderService)
		{
			_gameLogic = gameLogic; // TODO: Should not be here
			_services = services;
			_authenticationState = new AuthenticationState(_gameLogic, services, services.DataService, Trigger);
			_audioState = new AudioState(gameLogic, services, Trigger);
			_reconnection = new ReconnectionState(services, gameLogic, networkService, Trigger);
			_networkState = new NetworkState(gameLogic, services, networkService, Trigger);
			_tutorialState = new TutorialState(services, Trigger);
			_coreLoopState = new CoreLoopState(_reconnection, services, gameLogic, services.DataService, networkService, assetAdderService, Trigger,
				services.RoomService);
			_statechart = new Statechart.Statechart(Setup);
		}

		/// <inheritdoc cref="IStatechart.Run"/>
		public void Run()
		{
			_statechart.Run();
		}

		private void Trigger(IStatechartEvent eventTrigger)
		{
			_statechart.Trigger(eventTrigger);
		}

		public string GetCurrentStateDebug()
		{
			return _statechart.DebugCurrentState();
		}

		private void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var internetCheck = stateFactory.Choice("Internet Check");
			var authentication = stateFactory.Nest("Authentication");
			var core = stateFactory.Split("Core");

			initial.Transition().Target(internetCheck);
			initial.OnExit(() =>
			{
				if (BotCharacterSystem.Debug)
				{
					_services.UIService.OpenScreen<WorldDebugPresenter>().Forget();
				}
			});

			internetCheck.Transition().Condition(NetworkUtils.IsOffline).OnTransition(OpenNoInternetPopUp).Target(final);
			internetCheck.Transition().Target(authentication);

			authentication.Nest(_authenticationState.Setup).Target(core);

			core.Split(_networkState.Setup, _audioState.Setup, _tutorialState.Setup, _coreLoopState.Setup).Target(final);
		}

		private void OpenNoInternetPopUp()
		{
#if UNITY_EDITOR
			var desc = string.Format(ScriptLocalization.General.NoInternet);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.ExitGame,
				ButtonOnClick = () => { _services.QuitGame("Closing no internet popup"); }
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, desc, false, confirmButton);
#else
			var button = new FirstLight.NativeUi.AlertButton
			{
				Callback = () => { _services.QuitGame("Closing no internet popup"); },
				Style = FirstLight.NativeUi.AlertButtonStyle.Negative,
				Text = ScriptLocalization.General.ExitGame
			};

			FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.NoInternet,
				ScriptLocalization.General.NoInternetDescription, button);
#endif
		}
	}
}