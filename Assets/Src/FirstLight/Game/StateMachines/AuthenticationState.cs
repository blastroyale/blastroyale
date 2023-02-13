using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.SharedModels;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for player's authentication in the game in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class AuthenticationState
	{
		private readonly IStatechartEvent _goToRegisterClickedEvent = new StatechartEvent("Go To Register Clicked Event");
		private readonly IStatechartEvent _loginAsGuestEvent = new StatechartEvent("Login as Guest Event");
		private readonly IStatechartEvent _goToLoginClickedEvent = new StatechartEvent("Go To Login Clicked Event");
		private readonly IStatechartEvent _loginRegisterTransitionEvent = new StatechartEvent("Login Register Transition Clicked Event");
		private readonly IStatechartEvent _authSuccessEvent = new StatechartEvent("Authentication Success Event");
		private readonly IStatechartEvent _authFailEvent = new StatechartEvent("Authentication Fail Generic Event");
		private readonly IStatechartEvent _authFailContinueEvent = new StatechartEvent("Authentication Fail Continue Event");
		private readonly IStatechartEvent _authFailMaintenanceEvent = new StatechartEvent("Authentication Fail Account Deleted Event");
		private readonly IStatechartEvent _authFailOutdatedVersionEvent = new StatechartEvent("Authentication Fail Account Deleted Event");
		private readonly IStatechartEvent _authFailAccountDeletedEvent = new StatechartEvent("Authentication Fail Account Deleted Event");

		private readonly IGameDataProvider _dataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiServiceInit _uiService;
		private readonly IDataService _dataService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private IConfigsAdder _configsAdder;
		private string _passwordRecoveryEmailTemplateId = "";
		private string _lastUsedRecoveryEmail = "";
		private Task _asyncLogin = null;

		
		public AuthenticationState(IGameDataProvider dataProvider, IGameServices services, IGameUiServiceInit uiService,
								   IDataService dataService, Action<IStatechartEvent> statechartTrigger)
		{
			_dataProvider = dataProvider;
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
			initial.OnExit(SetupBackendEnvironmentData);

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

			postAuthCheck.Transition().Condition(IsAccountDeleted).Target(accountDeleted);
			postAuthCheck.Transition().Condition(IsGameInMaintenance).Target(gameBlocked);
			postAuthCheck.Transition().Condition(IsGameOutdated).Target(gameUpdate);
			postAuthCheck.Transition().Target(final);

			accountDeleted.WaitingFor(OpenAccountDeletedDialog).Target(authLoginGuest);

			gameBlocked.OnEnter(OpenGameBlockedDialog);

			gameUpdate.OnEnter(OpenGameUpdateDialog);

			final.OnEnter(UnsubscribeEvents);
		}
		
		private async Task WaitForAsyncLogin()
		{
			await _asyncLogin;
		}
		private async Task AsyncDeviceLogin()
		{
			bool complete = false;
			_services.AuthenticationService.LoginWithDevice(r =>
			{
				complete = true;
			}, (error) =>
			{
				OnAuthFail(error, true);
			});
			await WaitFor(() => complete);
		}
		
		private async Task AsyncGuestLogin()
		{
			bool complete = false;
			_services.AuthenticationService.LoginSetupGuest(r =>
			{
				complete = true;
			}, (error) =>
			{
				OnAuthFail(error, true);
			});
			await WaitFor(() => complete);
		}
		
		private async Task WaitFor(Func<bool> condition)
		{
			while (!condition()) await Task.Delay(2);
		}
		
		public async void QuickAsyncLogin()
		{
			_services.GameBackendService.SetupBackendEnvironment();
			if(HasLinkedDevice())
			{
				_asyncLogin = AsyncDeviceLogin();
			}
			else
			{
				_asyncLogin = AsyncGuestLogin();
			}
		}
		private bool IsAsyncLogin()
		{
			return _asyncLogin != null;
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
			return !string.IsNullOrWhiteSpace(_dataService.GetData<AppData>().DeviceId);
		}
		
		private void SetupBackendEnvironmentData()
		{
			_services.GameBackendService.SetupBackendEnvironment();
		}
		
		private void LoginWithDevice()
		{
			_services.AuthenticationService.LoginWithDevice(OnAuthSuccess, error =>
			{
				OnAuthFail(error, true);
			});
		}
		
		private void SetupLoginGuest()
		{
			_services.AuthenticationService.LoginSetupGuest(OnAuthSuccess, (error) =>
			{
				OnAuthFail(error, true);
			});
		}

		private void OnAuthSuccess(LoginData data)
		{
			_statechartTrigger(_authSuccessEvent);
		}

		private void OnAuthFail(PlayFabError error, bool automaticLogin)
		{
			_services.AuthenticationService.SetLinkedDevice(false);
			OnPlayFabError(error, automaticLogin);
			
			// If unauthorized/session ticket expired, try to re-log without moving the state machine
			if ((HttpStatusCode) error.HttpCode == HttpStatusCode.Unauthorized)
			{
				LoginWithDevice();
				return;
			}
			
			_statechartTrigger(_authFailEvent);
		}

		private void OnPlayFabError(PlayFabError error, bool automaticLogin)
		{
			string errorMessage = error.ErrorMessage;
			
			if (automaticLogin)
			{
				errorMessage = $"AutomaticLogin: {error.ErrorMessage}";
			}
			
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					_services.GenericDialogService.CloseDialog();
					_statechartTrigger(_authFailContinueEvent);
				}
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error("Authentication Fail - " + JsonConvert.SerializeObject(error.ErrorDetails));
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, errorMessage,
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
	}
}