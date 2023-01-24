using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
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
		private readonly IStatechartEvent _authFailEvent = new StatechartEvent("Authentication Fail Event");
		private readonly IStatechartEvent _authFailAccountDeletedEvent = new StatechartEvent("Authentication Fail Account Deleted Event");

		private readonly IGameDataProvider _dataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiServiceInit _uiService;
		private readonly IDataService _dataService;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private IConfigsAdder _configsAdder;
		private string _passwordRecoveryEmailTemplateId = "";
		private string _lastUsedRecoveryEmail = "";

		public AuthenticationState(IGameDataProvider dataProvider, IGameServices services, IGameUiServiceInit uiService,
			IDataService dataService,
			IGameBackendNetworkService networkService, Action<IStatechartEvent> statechartTrigger, IConfigsAdder cfgs)
		{
			_dataProvider = dataProvider;
			_services = services;
			_uiService = uiService;
			_dataService = dataService;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
			_configsAdder = cfgs;
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
			var authFail = stateFactory.Wait("Authentication Fail Dialog");

			initial.Transition().Target(autoAuthCheck);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(SetupBackendEnvironmentData);

			autoAuthCheck.Transition().Condition(HasLinkedDevice).Target(authLoginDevice);
			autoAuthCheck.Transition().Target(authLoginGuest);

			authFail.WaitingFor(ShowAuthFailDialog).Target(autoAuthCheck);
			
			authLoginGuest.OnEnter(SetupLoginGuest);
			authLoginGuest.Event(_authSuccessEvent).Target(final);
			authLoginGuest.Event(_authFailEvent).Target(authFail); // TODO - UNLINK DEVICE, SHOW LOGIN SCREEN?
			
			authLoginDevice.OnEnter(LoginWithDevice);
			authLoginDevice.Event(_authSuccessEvent).Target(final);
			authLoginDevice.Event(_authFailEvent).Target(authFail); // TODO - UNLINK DEVICE, SHOW LOGIN SCREEN?
			authLoginDevice.Event(_authFailAccountDeletedEvent).Target(authFail);
			
			//getServerState.WaitingFor(FinalStepsAuthentication).Target(accountStateCheck);

			final.OnEnter(UnsubscribeEvents);
		}

		private void ShowAuthFailDialog(IWaitActivity activity)
		{
			
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ServerHttpErrorMessage>(OnConnectionError);
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);
		}

		private void UnsubscribeEvents()
		{
			// TODO: Re-add the unsubscription when we can have global state for the authentication or just the re-login on the connection loss
			//_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		// TODO - REMOVE AUTHENTICATION UI SET FROM GAME COMPLETELY
		private void UnloadLoginRegisterScreens()
		{
			_uiService.UnloadUiSet((int)UiSetId.AuthenticationUi);
		}
		
		/// <summary>
		/// Create a new account by a random customID
		/// And links the current device to that account
		/// </summary>
		private void SetupLoginGuest()
		{
			_services.AuthenticationService.LoginSetupGuest(OnAuthSuccess, OnAuthFail);
		}

		private void OnAuthSuccess(LoginData data)
		{
			_statechartTrigger(_authSuccessEvent);
		}

		private void OnAuthFail(PlayFabError error)
		{
			_statechartTrigger(_authFailAccountDeletedEvent);
		}

		private void OnConnectionError(ServerHttpErrorMessage msg)
		{
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Session,
				"Invalid Session Ticket:" + msg.Message);

			if (msg.ErrorCode != HttpStatusCode.Unauthorized)
			{
				throw new PlayFabException(PlayFabExceptionCode.AuthContextRequired, msg.Message);
			}

			LoginWithDevice();

			_services.GameBackendService.CallFunction("GetPlayerData", res =>
				OnPlayerDataObtained(res, null), OnPlayFabError);
		}

		/// <summary>
		/// Callback for game-stopping errors. Prompts user to close the game.
		/// </summary>
		private void OnCriticalPlayFabError(PlayFabError error)
		{
			_services.AnalyticsService.CrashLog(error.ErrorMessage);
			var button = new AlertButton
			{
				Callback = () => { _services.QuitGame("Closing playfab critical error alert"); },
				Style = AlertButtonStyle.Negative,
				Text = ScriptLocalization.MainMenu.QuitGameButton
			};

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.MainMenu.PlayfabError, error.ErrorMessage, button);
		}

		private void OnPlayFabError(PlayFabError error)
		{
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Login,
				error.ErrorMessage);

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error("Authentication Fail - " + JsonConvert.SerializeObject(error.ErrorDetails));
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, error.ErrorMessage,
				false, confirmButton);
		}

		private void OnAuthenticationFail(PlayFabError error)
		{
			OnPlayFabError(error);
			_uiService.OpenUiAsync<LoginScreenBackgroundPresenter>();
			_statechartTrigger(_authFailEvent);
		}

		private void OnAutomaticAuthenticationFail(PlayFabError error)
		{
			_dataService.GetData<AppData>().DeviceId = null;
			_dataService.SaveData<AppData>();
			_uiService.OpenUiAsync<LoginScreenBackgroundPresenter>();
			_statechartTrigger(_authFailAccountDeletedEvent);
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Login,
				"AutomaticLogin:" + error.ErrorMessage);
		}

		private bool HasLinkedDevice()
		{
			return !string.IsNullOrWhiteSpace(_dataService.GetData<AppData>().DeviceId);
		}

		public void LoginWithDevice()
		{
			_services.AuthenticationService.LoginWithDevice(null, null);
		}

		private void SetupBackendEnvironmentData()
		{
			_services.GameBackendService.SetupBackendEnvironment();
		}

		private void ProcessAuthentication(LoginResult result)
		{
			var titleData = result.InfoResultPayload.TitleData;
			var appData = _dataService.GetData<AppData>();

			PlayFabSettings.staticPlayer.CopyFrom(result.AuthenticationContext);
			
			FLog.Verbose($"Logged in. PlayfabId={result.PlayFabId}");
			//AppleApprovalHack(result);

			if (!titleData.TryGetValue(GameConstants.PlayFab.VERSION_KEY, out var titleVersion))
			{
				throw new Exception($"{GameConstants.PlayFab.VERSION_KEY} not set in title data");
			}

			if (IsOutdated(titleVersion))
			{
				OpenGameUpdateDialog(titleVersion);
				return;
			}

			if (titleData.TryGetValue(GameConstants.PlayFab.MAINTENANCE_KEY, out var version) && IsOutdated(version))
			{
				OpenGameBlockedDialog();
				return;
			}

			FeatureFlags.ParseFlags(titleData);
			FeatureFlags.ParseLocalFeatureFlags();
			_services.LiveopsService.FetchSegments(_ =>
			{
				var liveopsFeatureFlags = _services.LiveopsService.GetUserSegmentedFeatureFlags();
				FeatureFlags.ParseFlags(liveopsFeatureFlags);
			});
			
			if (titleData.TryGetValue("PHOTON_APP", out var photonAppId))
			{
				var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;
				quantumSettings.AppSettings.AppIdRealtime = photonAppId;
				FLog.Verbose("Setting up photon app id by playfab title data");
			}

			_networkService.UserId.Value = result.PlayFabId;
			appData.DisplayName = result.InfoResultPayload.AccountInfo.TitleInfo.DisplayName;
			appData.FirstLoginTime = result.InfoResultPayload.AccountInfo.Created;
			appData.LoginTime = _services.TimeService.DateTimeUtcNow;
			appData.LastLoginTime = result.LastLoginTime ?? result.InfoResultPayload.AccountInfo.Created;
			appData.IsFirstSession = result.NewlyCreated;
			appData.PlayerId = result.PlayFabId;
			appData.LastLoginEmail = result.InfoResultPayload.AccountInfo.PrivateInfo.Email;

			if (FeatureFlags.REMOTE_CONFIGURATION)
			{
				FLog.Verbose("Parsing Remote Configurations");
				var remoteStringConfig = titleData[PlayfabConfigurationProvider.ConfigName];
				var serializer = new ConfigsSerializer();
				var remoteConfig = serializer.Deserialize<PlayfabConfigurationProvider>(remoteStringConfig);
				FLog.Verbose(
					$"Updating config from version {_configsAdder.Version.ToString()} to {remoteConfig.Version.ToString()}");
				_services.MessageBrokerService.Publish(new ConfigurationUpdate()
				{
					NewConfig = remoteConfig,
					OldConfig = _configsAdder
				});
				_configsAdder.UpdateTo(remoteConfig.Version, remoteConfig.GetAllConfigs());
			}
			_dataService.SaveData<AppData>();
			FLog.Verbose("Saved AppData");
			
			_services.AnalyticsService.SessionCalls.PlayerLogin(result.PlayFabId, _dataProvider.AppDataProvider.IsGuest);
		}

		private void FinalStepsAuthentication(IWaitActivity activity)
		{
			FLog.Verbose("Obtaining player data");
			
			_services.GameBackendService.CallFunction("GetPlayerData", res => OnPlayerDataObtained(res, activity),
				OnPlayFabError);

			PhotonAuthentication(activity.Split());
		}

		private void PhotonAuthentication(IWaitActivity activity)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var appId = config.PhotonServerSettings.AppSettings.AppIdRealtime;
			var request = new GetPhotonAuthenticationTokenRequest { PhotonApplicationId = appId };
			PlayFabClientAPI.GetPhotonAuthenticationToken(request, OnAuthenticationSuccess, OnCriticalPlayFabError);

			void OnAuthenticationSuccess(GetPhotonAuthenticationTokenResult result)
			{
				_networkService.QuantumClient.AuthValues.AddAuthParameter("token",
					result.PhotonCustomAuthenticationToken);
				activity.Complete();
			}
		}

		private void ShowAccountDeletedPopup()
		{
			var title = ScriptLocalization.UITSettings.account_deleted_title;
			var desc = ScriptLocalization.UITSettings.account_deleted_desc;
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.UITShared.ok,
				ButtonOnClick = () => { _services.QuitGame("Deleted User"); }
			};

			_services.GenericDialogService.OpenButtonDialog(title, desc, false, confirmButton);
		}

		

		/// <summary>
		/// Add all of the data in <paramref name="state"/> to the data service 
		/// </summary>
		private void AddDataToService(IWaitActivity activity, Dictionary<string, string> state)
		{
			foreach (var typeFullName in state.Keys)
			{
				try
				{
					var type = Assembly.GetExecutingAssembly().GetType(typeFullName);
					_dataService.AddData(type, ModelSerializer.DeserializeFromData(type, state));
				}
				catch (Exception e)
				{
					FLog.Error("Error reading data type "+typeFullName);
				}
			}
			
			activity?.Complete();
		}

		private void OnPlayerDataObtained(ExecuteFunctionResult res, IWaitActivity activity)
		{
			var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(res.FunctionResult.ToString());
			var data = serverResult.Result.Data;

			if (data == null || !data.ContainsKey(typeof(PlayerData).FullName)) // response too large, fetch directly
			{
				_services.GameBackendService.FetchServerState(state =>
				{
					AddDataToService(activity, state);
					FLog.Verbose("Downloaded state from playfab");
				});
				return;
			}

			AddDataToService(activity, data);
			FLog.Verbose("Downloaded state from server");
		}

		private void OpenGameUpdateDialog(string version)
		{
			var confirmButton = new AlertButton
			{
				Text = ScriptLocalization.General.Confirm,
				Style = AlertButtonStyle.Positive,
				Callback = OpenStore
			};

			var message = string.Format(ScriptLocalization.General.UpdateGame,
				VersionUtils.VersionExternal, version);

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

			var message = string.Format(ScriptLocalization.General.MaintenanceDescription,
				VersionUtils.VersionExternal);

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.Maintenance, message, confirmButton);
		}

		private void LoginClicked(string email, string password)
		{
			if (AuthenticationUtils.IsEmailFieldValid(email) && AuthenticationUtils.IsPasswordFieldValid(password))
			{
				_statechartTrigger(_loginRegisterTransitionEvent);
				
				_services.AuthenticationService.LoginWithEmail(email, password, OnLoginSuccess, OnAuthenticationFail);
			}
			else
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};

				string errorMessage =
					LocalizationManager.TryGetTranslation("UITLoginRegister/invalid_input", out var translation)
						? translation
						: $"#{"UITLoginRegister/invalid_input"}#";
				_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, errorMessage, false,
					confirmButton);
			}
		}

		// TODO - DEPRECATED, MOVE IT AWAY FROM HERE!!!
		private void RegisterClicked(string email, string username, string password)
		{
			if (AuthenticationUtils.IsUsernameFieldValid(username) 
			    && AuthenticationUtils.IsEmailFieldValid(email) 
			    && AuthenticationUtils.IsPasswordFieldValid(password))
			{
				_statechartTrigger(_loginRegisterTransitionEvent);

				var register = new RegisterPlayFabUserRequest
				{
					Email = email,
					DisplayName = username,
					Username = username.Replace(" ", ""),
					Password = password
				};
				PlayFabClientAPI.RegisterPlayFabUser(register, _ => LoginClicked(email, password),
					OnAuthenticationRegisterFail);
			}
			else
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};

				string errorMessage =
					LocalizationManager.TryGetTranslation("UITLoginRegister/invalid_input", out var translation)
						? translation
						: $"#{"UITLoginRegister/invalid_input"}#";
				_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, errorMessage, false,
					confirmButton);
			}
		}

		private void OpenLoadingScreen()
		{
			_uiService.OpenUi<LoadingScreenPresenter>();
		}

		private void OpenLoginScreen()
		{
			var data = new LoginScreenPresenter.StateData
			{
				LoginClicked = LoginClicked,
				GoToRegisterClicked = () => _statechartTrigger(_goToRegisterClickedEvent),
				PlayAsGuestClicked = () =>
				{
					_statechartTrigger(_loginAsGuestEvent);
				},
				ForgotPasswordClicked = SendRecoveryEmail
			};

			_uiService.OpenUiAsync<LoginScreenPresenter, LoginScreenPresenter.StateData>(data);
		}

		private void OpenRegisterScreen()
		{
			var data = new RegisterScreenPresenter.StateData
			{
				RegisterClicked = RegisterClicked,
				GoToLoginClicked = () => _statechartTrigger(_goToLoginClickedEvent)
			};

			_uiService.OpenUiAsync<RegisterScreenPresenter, RegisterScreenPresenter.StateData>(data);
		}

		private void SendRecoveryEmail(string email)
		{
			_lastUsedRecoveryEmail = email;
			
			SendAccountRecoveryEmailRequest request = new SendAccountRecoveryEmailRequest()
			{
				TitleId = PlayFabSettings.TitleId,
				Email = email,
				EmailTemplateId = _passwordRecoveryEmailTemplateId,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			PlayFabClientAPI.SendAccountRecoveryEmail(request, OnAccountRecoveryResult, OnPlayFabErrorSendRecoveryEmail);
		}

		private void OnAccountRecoveryResult(SendAccountRecoveryEmailResult result)
		{
			_services.GenericDialogService.CloseDialog();

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info,
				ScriptLocalization.MainMenu.SendPasswordEmailConfirm, false,
				confirmButton);
		}
		
		private void OnPlayFabErrorSendRecoveryEmail(PlayFabError error)
		{
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Login,
					error.ErrorMessage);

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					_services.GenericDialogService.CloseDialog();
					_uiService.GetUi<LoginScreenPresenter>().OpenPasswordRecoveryPopup(_lastUsedRecoveryEmail);
				}
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error("Authentication Fail - " + JsonConvert.SerializeObject(error.ErrorDetails));
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, error.ErrorMessage,
				false, confirmButton);
		}
		
		private bool IsOutdated(string version)
		{
			var appVersion = VersionUtils.VersionExternal.Split('.');
			var serverVersion = version.Split('.');
			var majorApp = int.Parse(appVersion[0]);
			var majorServer = int.Parse(serverVersion[0]);
			var minorApp = int.Parse(appVersion[1]);
			var minorServer = int.Parse(serverVersion[1]);
			var patchApp = int.Parse(appVersion[2]);
			var patchServer = int.Parse(serverVersion[2]);

			if (majorApp != majorServer)
			{
				return majorServer > majorApp;
			}

			if (minorApp != minorServer)
			{
				return minorServer > minorApp;
			}

			return patchServer > patchApp;
		}

		/// <summary>
		/// To help pass apple approval submission tests hack.
		/// This forces all communication with quantum to be TCP and not UDP with a flag from the backend, but just
		/// to be turned on during submission because sometimes Apple testers have their home network setup wrong.
		/// </summary>
		private async void AppleApprovalHack(LoginResult result)
		{
			var titleData = result.InfoResultPayload.TitleData;
			var address = AddressableId.Configs_Settings_QuantumRunnerConfigs.GetConfig().Address;
			var config = await _services.AssetResolverService.LoadAssetAsync<QuantumRunnerConfigs>(address);
			var connection = ConnectionProtocol.Udp;

			if (!titleData.TryGetValue($"{nameof(Application.version)} apple", out var version) ||
			    version != Application.version)
			{
				connection = ConnectionProtocol.Tcp;
			}

			config.PhotonServerSettings.AppSettings.Protocol = connection;

			_services.AssetResolverService.UnloadAsset(config);
		}

		private void OnApplicationQuit(ApplicationQuitMessage msg)
		{
			OpenLoadingScreen();
		}
	}
}