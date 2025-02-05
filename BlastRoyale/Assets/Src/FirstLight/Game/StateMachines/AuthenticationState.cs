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
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Services.Authentication.Hooks;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for player's authentication in the game in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class AuthenticationState
	{
		private const string VERSION_FILE_TEMPLATE = "https://cdn.blastroyale.com/versions/{0}.json";
		public static readonly int MaxAuthenticationRetries = 3;

		private readonly IStatechartEvent _authSuccessEvent = new StatechartEvent("Authentication Success Event");
		private readonly IStatechartEvent _authFailEvent = new StatechartEvent("Authentication Fail Generic Event");

		private readonly IStatechartEvent _authFailAccountDeletedEvent =
			new StatechartEvent("Authentication Fail Account Deleted Event");

		private readonly IGameServices _services;
		private readonly IDataService _dataService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private IConfigsAdder _configsAdder;
		private UniTask _asyncLogin;
		private bool _usingAsyncLogin;

		public class InnerState
		{
			public bool Success;
		}

		public InnerState State;

		public AuthenticationState(IGameServices services, IDataService dataService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataService = dataService;
			_statechartTrigger = statechartTrigger;
			State = new InnerState();
		}

		/// <summary>
		/// Setups the Initial Loading state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var authLoginGuest = stateFactory.State("Guest Login");
			var authFail = stateFactory.State("Authentication Fail Dialog");
			var postAuthCheck = stateFactory.Choice("Post Authentication Checks");
			var gameMaintenance = stateFactory.State("Game Blocked Dialog");
			var asyncLoginWait = stateFactory.TaskWait("Async Login Wait");

			initial.Transition().Target(asyncLoginWait);

			asyncLoginWait.WaitingFor(WaitForAsyncLogin).Target(postAuthCheck);

			postAuthCheck.Transition().Condition(() => !State.Success).Target(authFail);
			postAuthCheck.Transition().Condition(IsGameOutdatedOrMaintenance).Target(gameMaintenance);
			postAuthCheck.Transition().Target(final);

			gameMaintenance.OnEnter(OpenMaintenanceDialog);

			final.OnEnter(UnsubscribeEvents);
		}

		private async UniTask WaitForAsyncLogin()
		{
			await UniTask.WaitUntil(() => _asyncLogin.Status.IsCompleted());
		}

		public void QuickAsyncLogin()
		{
			_asyncLogin = LoginTask();
		}

		private async UniTask LoginTask()
		{
			FLog.Info("Starting async login");
			await _services.GameBackendService.SetupBackendEnvironment();
			var retries = 0;
			int maxRetries = 3;
			while (retries < maxRetries)
			{
				try
				{
					await _services.AuthService.AutomaticLogin();
					State.Success = true;
					return;
				}
				catch (Exception ex)
				{
					retries++;
					if (retries >= maxRetries)
					{
						State.Success = false;
						OnLoginError(ex);
						return;
					}

					FLog.Warn("Auth exception, retrying!", ex);
					await UniTask.Delay((int) (2000 * Math.Pow(2, retries - 1)));
				}
			}
		}

		private bool IsAuthErrorRecoverable(PlayFabError err)
		{
			return err.Error is not (PlayFabErrorCode.AccountDeleted
				or PlayFabErrorCode.AccountBanned
				or PlayFabErrorCode.AccountNotFound
				);
		}

		private void OnLoginError(Exception exception)
		{
			bool recoverable = true;
			var message = "";

			FLog.Error("Login Error", exception);

			if (exception is WrappedPlayFabException playFabException)
			{
				message = playFabException.Error.ErrorMessage;
				recoverable = IsAuthErrorRecoverable(playFabException.Error);
				if (playFabException.Error.ErrorDetails != null)
				{
					FLog.Error("Authentication Fail - " + JsonConvert.SerializeObject(playFabException.Error.ErrorDetails));
				}
			}
			else if (exception is AuthenticationException authenticationException)
			{
				message = authenticationException.Message;
				recoverable = authenticationException.Recoverable;
			}

			var account = _dataService.GetData<AccountData>();
			Analytics.SendEvent("loginError", new Dictionary<string, object>
			{
				{"report", exception.ToString()},
				{"device_id", account.DeviceId ?? "null"},
				{"last_login_email", account.LastLoginEmail ?? "null"},
				{"recoverable", recoverable},
			});

			if (!recoverable)
			{
				_services.AuthService.Logout().Forget();
			}

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					_services.QuitGame("OnLoginError");
				}
			};

			if (!recoverable)
			{
				message = $"<color=red>You will be logged out of this account!</color>\n\n<size=70%>{message}</size>";
			}
			else
			{
				message =
					$"{message}\n\nIf this keeps happening please contact us at <color=blue><u><a href=\"{GameConstants.Links.DISCORD_SERVER}\">Discord</a></u></color>";
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, message,
				false, confirmButton);
		}

		private void UnsubscribeEvents()
		{
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		private bool IsGameOutdatedOrMaintenance()
		{
			return _services.GameBackendService.IsGameInMaintenance(out _) || _services.GameBackendService.IsGameOutdated(out _);
		}

		private void OpenMaintenanceDialog()
		{
			_services.GameBackendService.IsGameInMaintenanceOrOutdated(true);
		}
	}
}