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
		private readonly IStatechartEvent _settingsCloseClickedEvent =
			new StatechartEvent("Settings Close Button Clicked Event");

		private readonly IStatechartEvent _logoutConfirmClickedEvent =
			new StatechartEvent("Logout Confirm Clicked Event");

		private readonly IStatechartEvent _logoutFailedEvent = new StatechartEvent("Logout Failed Event");
		
		private readonly MatchState _matchState;
		private readonly MainMenuState _mainMenuState;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly IAppLogic _appLogic;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private Coroutine _csPoolTimerCoroutine;

		public SettingsMenuState(IGameServices services, IGameLogic gameLogic, IGameUiService uiService, 
		                         Action<IStatechartEvent> statechartTrigger)
		{
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
			var logoutWait = stateFactory.State("Wait For Logout");

			initial.Transition().Target(settingsMenu);
			initial.OnExit(SubscribeEvents);
			
			settingsMenu.OnEnter(OpenSettingsMenuUI);
			settingsMenu.Event(_settingsCloseClickedEvent).Target(final);
			settingsMenu.Event(_logoutConfirmClickedEvent).Target(logoutWait);
			settingsMenu.OnExit(CloseSettingsMenuUI);

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
				OnClose = () => _statechartTrigger(_settingsCloseClickedEvent)
			};

			_uiService.OpenUi<SettingsScreenPresenter, SettingsScreenPresenter.StateData>(data);
		}

		private void CloseSettingsMenuUI()
		{
			_uiService.CloseUi<SettingsScreenPresenter>();
		}

		private void TryLogOut()
		{
#if UNITY_EDITOR
			var unlink = new UnlinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier
			};

			PlayFabClientAPI.UnlinkCustomID(unlink, OnUnlinkSuccess, OnUnlinkFail);

			void OnUnlinkSuccess(UnlinkCustomIDResult result)
			{
				UnlinkComplete();
			}
#elif UNITY_ANDROID
			var unlink = new UnlinkAndroidDeviceIDRequest
			{
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};
			
			PlayFabClientAPI.UnlinkAndroidDeviceID(unlink,OnUnlinkSuccess,OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkAndroidDeviceIDResult result)
			{
				UnlinkComplete();
			}
#elif UNITY_IOS
			var unlink = new UnlinkIOSDeviceIDRequest
			{
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};

			PlayFabClientAPI.UnlinkIOSDeviceID(unlink, OnUnlinkSuccess, OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkIOSDeviceIDResult result)
			{
				UnlinkComplete();
			}
#endif
		}
		
		private void OnUnlinkFail(PlayFabError error)
		{
			_services.AnalyticsService.CrashLog(error.ErrorMessage);

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () => { _statechartTrigger(_logoutFailedEvent); }
			};

			_services.GenericDialogService.OpenDialog(error.ErrorMessage, false, confirmButton);
		}
		
		private void UnlinkComplete()
		{
			_appLogic.LinkedEmail.Value = "";
			_appLogic.AccountLinkedStatus.Value = false;

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
					Callback = Application.Quit,
					Style = FirstLight.NativeUi.AlertButtonStyle.Positive,
					Text = ScriptLocalization.MainMenu.QuitGameButton
				};

				FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, ScriptLocalization.MainMenu.LogoutSuccessTitle,
				                               ScriptLocalization.MainMenu.LogoutSuccessDesc, button);
#endif
		}
	}
}