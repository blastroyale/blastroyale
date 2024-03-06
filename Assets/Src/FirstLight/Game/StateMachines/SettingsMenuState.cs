using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the settings menu in the <seealso cref="MainMenuState"/>
	/// </summary>
	public class SettingsMenuState
	{
		private readonly IStatechartEvent _settingsCloseClickedEvent = new StatechartEvent("Settings Close Button Clicked Event");
		private readonly IStatechartEvent _logoutConfirmClickedEvent = new StatechartEvent("Logout Confirm Clicked Event");
		private readonly IStatechartEvent _logoutFailedEvent = new StatechartEvent("Logout Failed Event");
		private readonly IStatechartEvent _connectIdClickedEvent = new StatechartEvent("Connect ID Clicked Event");
		private readonly IStatechartEvent _connectIdBackEvent = new StatechartEvent("Connect ID Back Event");
		private readonly IStatechartEvent _connectIdLoginSuccessEvent = new StatechartEvent("Connect ID login success event");
		private readonly IStatechartEvent _connectIdRegisterSuccessEvent = new StatechartEvent("Connect ID register success event");
		private readonly IStatechartEvent _connectIdFailedEvent = new StatechartEvent("Connect ID failed event");
		private readonly IStatechartEvent _exitedSelectServer = new StatechartEvent("Exited Select Server");

		private readonly MatchState _matchState;
		private readonly MainMenuState _mainMenuState;
		private readonly IGameDataProvider _data;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly IAppLogic _appLogic;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private Coroutine _csPoolTimerCoroutine;

		public SettingsMenuState(IGameDataProvider data, IGameServices services, IGameLogic gameLogic,
								 IGameUiService uiService,
								 Action<IStatechartEvent> statechartTrigger)
		{
			_data = data;
			_services = services;
			_uiService = uiService;
			_appLogic = gameLogic.AppLogic;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var settingsMenu = stateFactory.State("Settings Menu");
			var connectId = stateFactory.State("Connect ID Screen");
			var serverSelect = stateFactory.State("Server Select");
			var logoutWait = stateFactory.State("Wait For Logout");

			initial.Transition().Target(settingsMenu);
			initial.OnExit(SubscribeEvents);

			settingsMenu.OnEnter(OpenSettingsScreen);
			settingsMenu.Event(_settingsCloseClickedEvent).Target(final);
			settingsMenu.Event(_logoutConfirmClickedEvent).Target(logoutWait);
			settingsMenu.Event(_connectIdClickedEvent).Target(connectId);
			settingsMenu.Event(NetworkState.OpenServerSelectScreenEvent).Target(serverSelect);

			connectId.OnEnter(OpenConnectIdUI);
			connectId.Event(_connectIdBackEvent).Target(settingsMenu);
			connectId.Event(_connectIdRegisterSuccessEvent).OnTransition(UpdateAccountStatus).Target(settingsMenu);
			connectId.Event(_connectIdLoginSuccessEvent).OnTransition(UpdateAccountStatus).Target(settingsMenu);
			connectId.Event(_connectIdFailedEvent).Target(settingsMenu);
			connectId.OnExit(CloseConnectUI);

			serverSelect.OnEnter(() => _ = OpenServerSelectUI());
			serverSelect.Event(_exitedSelectServer).Target(settingsMenu);

			logoutWait.OnEnter(TryLogOut);
			logoutWait.Event(_logoutFailedEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void UpdateAccountStatus()
		{
			if (_uiService.HasUiPresenter<SettingsScreenPresenter>())
			{
				_uiService.GetUi<SettingsScreenPresenter>().UpdateAccountStatus();
			}
		}

		private void CloseSettingsScreen()
		{
			_uiService.CloseCurrentScreen();
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ServerHttpErrorMessage>(OnServerHttpErrorMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OpenSettingsScreen()
		{
			var data = new SettingsScreenPresenter.StateData
			{
				LogoutClicked = TryLogOut,
				OnClose = () => _statechartTrigger(_settingsCloseClickedEvent),
				OnServerSelectClicked = () => _statechartTrigger(NetworkState.OpenServerSelectScreenEvent),
				OnConnectIdClicked = () => _statechartTrigger(_connectIdClickedEvent),
				OnCustomizeHudClicked = CustomizeHud,
				OnDeleteAccountClicked = () =>
					_services.GameBackendService.CallFunction("RemovePlayerData", OnAccountDeleted, null)
			};

			_uiService.OpenScreen<SettingsScreenPresenter, SettingsScreenPresenter.StateData>(data);
		}

		private void CustomizeHud()
		{
			_uiService.CloseUi<SettingsScreenPresenter>();
			_uiService.OpenScreen<HudCustomizationScreenPresenter, HudCustomizationScreenPresenter.StateData>(new ()
			{
				OnClose = () =>
				{
					_uiService.CloseUi<SettingsScreenPresenter>(true);
					_uiService.OpenScreen<SettingsScreenPresenter>();
				},
				OnSave = e =>
				{
					_services.ControlsSetup.SaveControlsPositions(e);
					_uiService.CloseUi<SettingsScreenPresenter>(true);
					_uiService.OpenScreen<SettingsScreenPresenter>();
				}
			});
		}

		private async UniTaskVoid OpenServerSelectUI()
		{
			var data = new ServerSelectScreenPresenter.StateData
			{
				OnExit = (changed) =>
				{
					if (changed)
					{
						_services.MessageBrokerService.Publish(new ChangedServerRegionMessage());
						_services.GenericDialogService.OpenSimpleMessage("Server", "Connected to " + _appLogic.ConnectionRegion.Value.GetPhotonRegionTranslation() + " server!");
					}

					_statechartTrigger(_exitedSelectServer);
				},
			};

			await _uiService.OpenUiAsync<ServerSelectScreenPresenter, ServerSelectScreenPresenter.StateData>(data);
		}

		private void CloseServerSelectUI()
		{
			_uiService.CloseUi<ServerSelectScreenPresenter>(true);
		}


		private async void OpenConnectIdUI()
		{
			var data = new ConnectFlgIdScreenPresenter.StateData
			{
				CloseClicked = () => _statechartTrigger(_connectIdBackEvent),
				AuthLoginSuccess = () => _statechartTrigger(_connectIdLoginSuccessEvent),
				AuthLoginFail = () => _statechartTrigger(_connectIdFailedEvent),
				AuthRegisterSuccess = () => _statechartTrigger(_connectIdRegisterSuccessEvent)
			};

			await _uiService.OpenUiAsync<ConnectFlgIdScreenPresenter, ConnectFlgIdScreenPresenter.StateData>(data);
		}

		private void CloseConnectUI()
		{
			_uiService.CloseUi<ConnectFlgIdScreenPresenter>();
		}

		private void TryLogOut()
		{
			_services.AuthenticationService.Logout(OnLogoutComplete, OnLogoutFail);
			_uiService.OpenUi<LoadingSpinnerScreenPresenter>();
		}

		private void OnLogoutComplete()
		{
			_uiService.CloseUi<LoadingSpinnerScreenPresenter>();

			var title = ScriptLocalization.UITShared.info;
			var desc = ScriptLocalization.UITSettings.logout_success_desc;

#if UNITY_EDITOR
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.MainMenu.QuitGameButton,
				ButtonOnClick = () => { _services.QuitGame("Closing due to logout"); }
			};

			_services.GenericDialogService.OpenButtonDialog(title, desc, false, confirmButton);
#else
				var button = new FirstLight.NativeUi.AlertButton
				{
					Callback = () => {_services.QuitGame("Closing unlink complete alert"); },
					Style = FirstLight.NativeUi.AlertButtonStyle.Positive,
					Text = ScriptLocalization.MainMenu.QuitGameButton
				};

				FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, title, desc, button);
#endif
		}

		private void OnLogoutFail(PlayFabError error)
		{
			_uiService.CloseUi<LoadingSpinnerScreenPresenter>();

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.MainMenu.QuitGameButton,
				ButtonOnClick = () => { _services.QuitGame("Failed to log out"); }
			};

			_services.GenericDialogService.OpenButtonDialog("Logout Fail", error.ErrorMessage, false, confirmButton);
		}

		private void OpenGameClosePopup()
		{
			var title = "Login Success";
			var desc = "Please close the game to continue using your account.";

#if UNITY_EDITOR
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.MainMenu.QuitGameButton,
				ButtonOnClick = () => { _services.QuitGame("Closing due to logout"); }
			};

			_services.GenericDialogService.OpenButtonDialog(title, desc, false, confirmButton);
#else
				var button = new FirstLight.NativeUi.AlertButton
				{
					Callback = () => {_services.QuitGame("Closing unlink complete alert"); },
					Style = FirstLight.NativeUi.AlertButtonStyle.Positive,
					Text = ScriptLocalization.MainMenu.QuitGameButton
				};

				FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, title, desc, button);
#endif
		}

		private void OnServerHttpErrorMessage(ServerHttpErrorMessage msg)
		{
			_services.AnalyticsService.CrashLog(msg.Message);

#if UNITY_EDITOR
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () => { _statechartTrigger(_logoutFailedEvent); }
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, msg.Message, false,
				confirmButton);
#else
			var button = new NativeUi.AlertButton
			{
				Callback = () => { _statechartTrigger(_logoutFailedEvent); },
				Style = NativeUi.AlertButtonStyle.Default,
				Text = ScriptLocalization.General.OK
			};

			NativeUi.NativeUiService.ShowAlertPopUp(false,ScriptLocalization.MainMenu.PlayfabError, msg.Message, button);
#endif
		}

		private void OnAccountDeleted(ExecuteFunctionResult res)
		{
			TryLogOut();
		}
	}
}