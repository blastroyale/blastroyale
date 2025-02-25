using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
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
using FirstLight.UIService;
using FirstLightServerSDK.Services;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using Unity.Services.UserReporting;
using Unity.Services.UserReporting.Client;
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

		private readonly GameLogic _gameLogic;
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

		public AuthenticationState(GameLogic gameLogic, IGameServices services, IDataService dataService, Action<IStatechartEvent> statechartTrigger)
		{
			_gameLogic = gameLogic;
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
			var authFail = stateFactory.State("Authentication Fail Dialog");
			var postAuthCheck = stateFactory.Choice("Post Authentication Checks");
			var gameMaintenance = stateFactory.State("Game Blocked Dialog");
			var asyncLoginWait = stateFactory.TaskWait("Async Login Wait");

			initial.Transition().Target(asyncLoginWait);
			asyncLoginWait.WaitingFor(LoginFlow).Target(postAuthCheck);
			postAuthCheck.Transition().Condition(() => !State.Success).Target(authFail);
			postAuthCheck.Transition().Condition(IsGameOutdatedOrMaintenance).Target(gameMaintenance);
			postAuthCheck.Transition().Target(final);

			gameMaintenance.OnEnter(OpenMaintenanceDialog);

			final.OnEnter(UnsubscribeEvents);
		}

		private async UniTask LoginFlow()
		{
			await _services.GameBackendService.SetupBackendEnvironment();

			if (!_services.LocalPrefsService.AcceptedPrivacyTerms)
			{
				LoadingScreenPresenter.Hide();
				GlobalAnimatedBackground.Instance.SetDimmedColor();
				// Wait for accepting terms
				await OpenPrivacyDialogAndWaitForAccept();
				_services.LocalPrefsService.AcceptedPrivacyTerms.Value = true;
			}

			// user already logged iin into the game and can automatically login again with the device id
			var loggedIn = false;
			if (_services.AuthService.HasDeviceLinked())
			{
				await AuthenticateAndInitializeLogic(_services.AuthService.LoginWithDeviceID);
				if (await _services.AuthService.CanLoginWithNativeGamesService())
				{
					_services.AuthService.TryToLinkNativeAccount().Forget();
				}

				loggedIn = true;
			}
			// Try to login with native game service
			else if (await _services.AuthService.CanLoginWithNativeGamesService())
			{
				FLog.Info("Can login with native game service");
				await AuthenticateAndInitializeLogic(_services.AuthService.LoginWithNativeGamesService);
				FLog.Info("logged in with native game service");
				loggedIn = true;
			}

			await WaitForSkipTutorialInput(loggedIn);
			GlobalAnimatedBackground.Instance.Disable();
		}

		private async Task WaitForSkipTutorialInput(bool loggedIn)
		{
			var loggedInWithUserPassword = false;
			while (true)
			{
				if (loggedIn) // user already finished the tutorial
				{
					if (_services.TutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH))
					{
						if (_services.UIService.IsScreenOpen<SkipTutorialPopupPresenter>())
						{
							await _services.UIService.CloseScreen<SkipTutorialPopupPresenter>();
						}

						return;
					}
				}

				GlobalAnimatedBackground.Instance.SetDimmedColor();
				LoadingScreenPresenter.Hide();

				// First time user opening the game lets see if he wants to do the tutorial. I surely don't want to do it again
				var skipTutorialScreen = await _services.UIService.OpenScreen<SkipTutorialPopupPresenter>();
				if (loggedInWithUserPassword) // User may have logged in and not finished tutorial so lets remove
				{
					skipTutorialScreen.DisableLoginOption();
				}

				var result = await skipTutorialScreen.WaitForResult();
				if (result == SkipTutorialPopupPresenter.AllowedOptions.Login)
				{
					var loginResult = await LoginScreenFlow();
					if (loginResult == null) // User did not login and closed the screen
					{
						continue;
					}

					LoadingScreenPresenter.Show();
					await AuthenticateAndInitializeLogic(() => _services.AuthService.LoginWithExistingResult(loginResult));
					if (await _services.AuthService.CanLoginWithNativeGamesService())
					{
						_services.AuthService.TryToLinkNativeAccount().Forget();
					}

					loggedInWithUserPassword = true;
					loggedIn = true;
				}

				else if (result is SkipTutorialPopupPresenter.AllowedOptions.SkipTutorial or SkipTutorialPopupPresenter.AllowedOptions.Tutorial)
				{
					await skipTutorialScreen.Close();
					LoadingScreenPresenter.Show();

					if (!loggedIn) // Users may already have logged in automatically with native games services
					{
						await AuthenticateAndInitializeLogic(_services.AuthService.LoginWithGuestAccount);
					}

					if (result == SkipTutorialPopupPresenter.AllowedOptions.SkipTutorial)
					{
						_services.TutorialService.SkipTutorial();
					}

					return;
				}
				else
				{
					throw new Exception("I don't know this yet!");
				}
			}
		}

		private async Task OpenPrivacyDialogAndWaitForAccept()
		{
			var privacyAcceptedSrc = new UniTaskCompletionSource();
			await _services.UIService.OpenScreen<PrivacyDialogPresenter>(new PrivacyDialogPresenter.StateData()
			{
				OnAccept = () => privacyAcceptedSrc.TrySetResult()
			});
			await privacyAcceptedSrc.Task;
			await _services.UIService.CloseScreen<PrivacyDialogPresenter>();
		}

		private async UniTask<LoginResult> LoginScreenFlow()
		{
			var screen = await _services.UIService.OpenScreen<EmailPasswordLoginPopupPresenter>();

			while (true)
			{
				var result = await screen.WaitForResult();
				screen.ResetResult();

				switch (result.ResultActionType)
				{
					case EmailPasswordLoginPopupPresenter.Result.ResultType.Close:
						await screen.Close();
						return null;
					case EmailPasswordLoginPopupPresenter.Result.ResultType.ResetPassword:
						try
						{
							await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
							await _services.AuthService.SendAccountRecoveryEmail(result.Email);
							_services.InGameNotificationService.QueueNotification(ScriptLocalization.UITLoginRegister.reset_password_confirm);
						}
						catch (Exception ex)
						{
							FLog.Error("Failed to send recovery email", ex);
							_services.InGameNotificationService.QueueNotification("Failed to send recovery email, try again.");
						}
						finally
						{
							await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
						}

						continue;

					case EmailPasswordLoginPopupPresenter.Result.ResultType.Login:
						try
						{
							await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
							var loginResult = await _services.AuthService.FetchCredentialsWithEmailPassword(result.Email, result.Password);
							await screen.Close();
							return loginResult;
						}
						catch (Exception ex)
						{
							if (ex is WrappedPlayFabException playFabException)
							{
								screen.ClearPasswordField();
								_services.InGameNotificationService.QueueNotification(playFabException.Error.ErrorMessage);
							}
							else
							{
								FLog.Error("Failed to send recovery email", ex);
								_services.InGameNotificationService.QueueNotification("Failed to login, try again!");
							}

							continue;
						}
						finally
						{
							await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
						}
				}

				return null;
			}
		}

		private async UniTask AuthenticateAndInitializeLogic(Func<UniTask> loginMethod)
		{
			FLog.Info("Starting async login");
			var retries = 0;
			int maxRetries = 3;
			while (retries < maxRetries)
			{
				try
				{
					await loginMethod();
					InitializeGameLogic();
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
						throw ex;
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

		private void InitializeGameLogic()
		{
			_gameLogic.Init();
			_services.MessageBrokerService.Publish(new LogicInitializedMessage());
			InitUserReporting(_services, _gameLogic.RemoteConfigProvider).Forget(); // TODO: Move this to Startup when we can await for auth there
		}

		private static async UniTaskVoid InitUserReporting(IGameServices services, IRemoteConfigProvider remoteConfig)
		{
			if (!remoteConfig.GetConfig<GeneralConfig>().ShowBugReportButton) return;

			var customConfig = new UserReportingClientConfiguration(100, 300, 60, 1);
			UserReportingService.Instance.Configure(customConfig);

			await services.UIService.OpenScreen<UserReportScreenPresenter>();
		}
	}
}