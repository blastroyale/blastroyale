using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
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
		
		private readonly MatchState _matchState;
		private readonly MainMenuState _mainMenuState;
		private readonly IGameDataProvider _data;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly IAppLogic _appLogic;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private Coroutine _csPoolTimerCoroutine;

		public SettingsMenuState(IGameDataProvider data, IGameServices services, IGameLogic gameLogic, IGameUiService uiService, 
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
			var logoutWait = stateFactory.State("Wait For Logout");

			initial.Transition().Target(settingsMenu);
			initial.OnExit(SubscribeEvents);
			
			settingsMenu.OnEnter(OpenSettingsMenuUI);
			settingsMenu.Event(_settingsCloseClickedEvent).Target(final);
			settingsMenu.Event(_logoutConfirmClickedEvent).Target(logoutWait);
			settingsMenu.Event(_connectIdClickedEvent).Target(connectId);
			settingsMenu.OnExit(CloseSettingsMenuUI);
			
			connectId.OnEnter(OpenConnectIdScreen);
			connectId.Event(_connectIdBackEvent).Target(settingsMenu);
			connectId.OnExit(CloseConnectIdScreen);
			
			logoutWait.OnEnter(TryLogOut);
			logoutWait.Event(_logoutFailedEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OpenSettingsMenuUI()
		{
			var data = new SettingsScreenPresenter.StateData
			{
				LogoutClicked = TryLogOut,
				OnClose = () => _statechartTrigger(_settingsCloseClickedEvent),
				OnServerSelectClicked = () => _statechartTrigger(NetworkState.OpenServerSelectScreenEvent),
				OnConnectIdClicked = () => _statechartTrigger(_connectIdClickedEvent)
			};

			_uiService.OpenUiAsync<SettingsScreenPresenter, SettingsScreenPresenter.StateData>(data);
		}

		private void CloseSettingsMenuUI()
		{
			_uiService.CloseUi<SettingsScreenPresenter>(false, true);
		}

		private void OpenConnectIdScreen()
		{
			var data = new ConnectIdScreenPresenter.StateData
			{
				ConnectClicked = TryConnectId,
				BackClicked = () => _statechartTrigger(_connectIdBackEvent)
			};

			_uiService.OpenUiAsync<ConnectIdScreenPresenter, ConnectIdScreenPresenter.StateData>(data);
		}

		private void CloseConnectIdScreen()
		{
			_uiService.CloseUi<ConnectIdScreenPresenter>();
		}

		private void SetConnectIdDim(bool activateDim)
		{
			if (_uiService.HasUiPresenter<ConnectIdScreenPresenter>())
			{
				_uiService.GetUi<ConnectIdScreenPresenter>().SetFrontDimBlockerActive(activateDim);
			}
		}

		private void TryConnectId(string email, string password, string username)
		{
			SetConnectIdDim(true);
			_services.PlayfabService.AttachLoginDataToAccount(email, password, username, OnConnectIdComplete,
			                                               OnPlayfabError);
		}

		private void TryLogOut()
		{
			_services.PlayfabService.UnlinkDeviceID(OnUnlinkComplete, OnPlayfabError);
		}

		private void OnConnectIdComplete(AddUsernamePasswordResult result)
		{
			SetConnectIdDim(false);
			_statechartTrigger(_connectIdBackEvent);
		}
		
		private void OnUnlinkComplete()
		{
			_services.HelpdeskService.Logout();
			
#if UNITY_EDITOR
			var title = string.Format(ScriptLocalization.MainMenu.LogoutSuccessDesc);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.MainMenu.QuitGameButton,
				ButtonOnClick = () => { UnityEditor.EditorApplication.isPlaying = false; }
			};

			_services.GenericDialogService.OpenDialog(title, false, confirmButton);
			return;
#else
				var button = new FirstLight.NativeUi.AlertButton
				{
					Callback = () =>
					{
						_services.QuitGame("Closing unlink complete alert");
					},
					Style = FirstLight.NativeUi.AlertButtonStyle.Positive,
					Text = ScriptLocalization.MainMenu.QuitGameButton
				};

				FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, ScriptLocalization.MainMenu.LogoutSuccessTitle,
				                               ScriptLocalization.MainMenu.LogoutSuccessDesc, button);
#endif
		}
		
		private void OnPlayfabError(PlayFabError error)
		{
			_services.AnalyticsService.CrashLog(error.ErrorMessage);

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () => { _statechartTrigger(_logoutFailedEvent); }
			};

			_services.GenericDialogService.OpenDialog(error.ErrorMessage, false, confirmButton);
		}
	}
}