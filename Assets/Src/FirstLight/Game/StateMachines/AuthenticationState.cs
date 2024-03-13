using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;
using UnityEngine.Analytics;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for player's authentication in the game in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class AuthenticationState
	{
		public static readonly int MaxAuthenticationRetries = 3;

		private readonly IStatechartEvent _authSuccessEvent = new StatechartEvent("Authentication Success Event");
		private readonly IStatechartEvent _authFailEvent = new StatechartEvent("Authentication Fail Generic Event");

		private readonly IStatechartEvent _authFailContinueEvent =
			new StatechartEvent("Authentication Fail Continue Event");

		private readonly IStatechartEvent _authFailAccountDeletedEvent =
			new StatechartEvent("Authentication Fail Account Deleted Event");

		private readonly IGameServices _services;
		private readonly IGameUiServiceInit _uiService;
		private readonly IDataService _dataService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private IConfigsAdder _configsAdder;
		private UniTask _asyncLogin;
		private bool _usingAsyncLogin;

		public AuthenticationState(IGameServices services, IGameUiServiceInit uiService,
								   IDataService dataService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_dataService = dataService;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Initial Loading state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var setupEnvironment = stateFactory.Transition("Setup Environment");
			var authLoginGuest = stateFactory.State("Guest Login");
			var autoAuthCheck = stateFactory.Choice("Auto Auth Check");
			var authLoginDevice = stateFactory.State("Login Device Authentication");
			var authFail = stateFactory.State("Authentication Fail Dialog");
			var postAuthCheck = stateFactory.Choice("Post Authentication Checks");
			var accountDeleted = stateFactory.Wait("Account Deleted Dialog");
			var gameBlocked = stateFactory.State("Game Blocked Dialog");
			var gameUpdate = stateFactory.State("Game Update Dialog");
			var asyncLoginWait = stateFactory.TaskWait("Async Login Wait");

			initial.Transition().Target(autoAuthCheck);
			initial.OnExit(SubscribeEvents);

			setupEnvironment.OnEnter(SetupBackendEnvironmentData);
			setupEnvironment.Transition().Target(autoAuthCheck);

			autoAuthCheck.Transition().Condition(IsAsyncLogin).Target(asyncLoginWait);
			autoAuthCheck.Transition().Condition(HasLinkedDevice).Target(authLoginDevice);
			autoAuthCheck.Transition().Target(authLoginGuest);

			authFail.Event(_authFailContinueEvent).Target(autoAuthCheck);

			asyncLoginWait.WaitingFor(WaitForAsyncLogin).Target(postAuthCheck);

			authLoginGuest.OnEnter(SetupLoginGuest);
			authLoginGuest.Event(_authSuccessEvent).Target(final);
			authLoginGuest.Event(_authFailEvent).Target(authFail);

			authLoginDevice.OnEnter(LoginWithDevice);
			authLoginDevice.Event(_authSuccessEvent).Target(final);
			authLoginDevice.Event(_authFailEvent).Target(authFail);
			authLoginDevice.Event(_authFailAccountDeletedEvent).Target(authFail);

			postAuthCheck.Transition().Condition(() => _services.AuthenticationService.State.LastAttemptFailed).Target(authFail);
			postAuthCheck.Transition().Condition(IsAccountDeleted).Target(accountDeleted);
			postAuthCheck.Transition().Condition(IsGameInMaintenance).Target(gameBlocked);
			postAuthCheck.Transition().Condition(IsGameOutdated).Target(gameUpdate);
			postAuthCheck.Transition().Target(final);

			accountDeleted.WaitingFor(OpenAccountDeletedDialog).Target(authLoginGuest);

			gameBlocked.OnEnter(OpenGameBlockedDialog);

			gameUpdate.OnEnter(OpenGameUpdateDialog);

			final.OnEnter(PublishAuthenticationSuccessMessage);
			final.OnEnter(UnsubscribeEvents);
		}


		private async UniTask WaitForAsyncLogin()
		{
			await UniTask.WaitUntil(() => _asyncLogin.Status.IsCompleted());
		}

		private async UniTask AsyncDeviceLogin()
		{
			bool complete = false;
			_services.AuthenticationService.State.StartedWithAccount = true;
			_services.AuthenticationService.LoginWithDevice(r => { complete = true; },
				(error) =>
				{
					OnAuthFail(error, true);
					complete = true;
				});
			await UniTask.WaitUntil(() => complete);
		}

		private async UniTask AsyncGuestLogin()
		{
			bool complete = false;
			_services.AuthenticationService.LoginSetupGuest(r => { complete = true; },
				(error) => { OnAuthFail(error, true); });
			await UniTask.WaitUntil(() => complete);
		}

		public void QuickAsyncLogin()
		{
			_services.GameBackendService.SetupBackendEnvironment();
			_usingAsyncLogin = true;
			_asyncLogin = HasLinkedDevice() ? AsyncDeviceLogin() : AsyncGuestLogin();
		}

		private bool IsAsyncLogin()
		{
			return _usingAsyncLogin;
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);
		}


		private void UnsubscribeEvents()
		{
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		private bool IsAccountDeleted()
		{
			return _services.AuthenticationService.IsAccountDeleted();
		}

		private bool IsGameInMaintenance()
		{
			return _services.GameBackendService.IsGameInMaintenance();
		}

		private bool IsGameOutdated()
		{
			return _services.GameBackendService.IsGameOutdated();
		}

		private bool HasLinkedDevice()
		{
			return !string.IsNullOrWhiteSpace(_services.AuthenticationService.GetDeviceSavedAccountData().DeviceId);
		}

		private void SetupBackendEnvironmentData()
		{
			_services.GameBackendService.SetupBackendEnvironment();
		}

		private void LoginWithDevice()
		{
			_services.AuthenticationService.LoginWithDevice(OnAuthSuccess, error => { OnAuthFail(error, true); });
		}

		private void SetupLoginGuest()
		{
			_services.AuthenticationService.LoginSetupGuest(OnAuthSuccess, (error) => { OnAuthFail(error, true); });
		}

		private void OnAuthSuccess(LoginData data)
		{
			_services.AuthenticationService.State.LastAttemptFailed = false;
			_statechartTrigger(_authSuccessEvent);
		}

		private void OnAuthFail(PlayFabError error, bool automaticLogin)
		{
			var hasAccount = _services.AuthenticationService.State.StartedWithAccount;
			_services.AuthenticationService.State.LastAttemptFailed = true;

			// If unauthorized/session ticket expired, try to re-log without moving the state machine
			var recoverable = IsAuthErrorRecoverable(error);

			// Some unrecoverable shit happen, show a button to clear account data and reopen the game.
			// Send error
			OnLoginError(error);


			return;
		}

		private bool IsAuthErrorRecoverable(PlayFabError err)
		{
			return err.Error is not (PlayFabErrorCode.AccountDeleted
				or PlayFabErrorCode.AccountBanned
				or PlayFabErrorCode.AccountNotFound
				);
		}

		private void OnLoginError(PlayFabError error)
		{
			var recoverable = IsAuthErrorRecoverable(error);

			var account = _dataService.GetData<AccountData>();
			Analytics.SendEvent("loginError", new Dictionary<string, object>
			{
				{"report", error.GenerateErrorReport()},
				{"device_id", account.DeviceId ?? "null"},
				{"last_login_email", account.LastLoginEmail ?? "null"},
				{"recoverable", recoverable},
			});
			if (error.ErrorDetails != null)
			{
				FLog.Error("Authentication Fail - " + JsonConvert.SerializeObject(error.ErrorDetails));
			}

			FLog.Info("Error: "+error.Error);
			
			if (!recoverable)
			{
				_services.AuthenticationService.SetLinkedDevice(false);
			}

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					_services.QuitGame("OnLoginError " + error.GenerateErrorReport());
				}
			};

			var message = error.ErrorMessage;
			if (!recoverable)
			{
				message = $"<color=red>You will be logged out of this account!</color>\n\n<size=70%>{message}</size>";
			}
			else
			{
				message = $"{message}\n\nIf this keeps happening please contact us at <color=blue><u><a href=\"{GameConstants.Links.DISCORD_SERVER}\">Discord</a></u></color>";
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, message,
				false, confirmButton);
		}

		private void OpenAccountDeletedDialog(IWaitActivity activity)
		{
			var title = ScriptLocalization.UITSettings.account_deleted_title;
			var desc = ScriptLocalization.UITSettings.account_deleted_desc;
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.UITShared.ok,
				ButtonOnClick = () =>
				{
					activity.Complete();
					_services.QuitGame("Deleted User");
				}
			};

			_services.GenericDialogService.OpenButtonDialog(title, desc, false, confirmButton);
		}

		private void OpenGameUpdateDialog()
		{
			var confirmButton = new AlertButton
			{
				Text = ScriptLocalization.General.Confirm,
				Style = AlertButtonStyle.Positive,
				Callback = OpenStore
			};


			var message = string.Format(ScriptLocalization.General.UpdateGame, VersionUtils.VersionExternal,
				_services.GameBackendService.GetTitleVersion());

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.NewGameUpdate, message, confirmButton);

			void OpenStore()
			{
#if UNITY_IOS
				Application.OpenURL(GameConstants.Links.APP_STORE_IOS);
#elif UNITY_ANDROID
				Application.OpenURL(GameConstants.Links.APP_STORE_GOOGLE_PLAY);
#endif
			}
		}

		private void OpenGameBlockedDialog()
		{
			var confirmButton = new AlertButton
			{
				Text = ScriptLocalization.General.Confirm,
				Style = AlertButtonStyle.Default,
				Callback = () => { _services.QuitGame("Closing game blocked dialog"); }
			};

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.Maintenance,
				ScriptLocalization.General.MaintenanceDescription, confirmButton);
		}

		private void OpenLoadingScreen()
		{
			_uiService.OpenUi<LoadingScreenPresenter>();
		}

		private void OnApplicationQuit(ApplicationQuitMessage msg)
		{
			OpenLoadingScreen();
		}

		private void PublishAuthenticationSuccessMessage()
		{
			_services.MessageBrokerService.Publish(new SuccessAuthentication());
		}
	}
}