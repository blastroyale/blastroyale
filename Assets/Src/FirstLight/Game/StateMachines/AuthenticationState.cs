using System;
using System.Net;
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
		private readonly IStatechartEvent _loginCompletedEvent = new StatechartEvent("Login Completed Event");
		private readonly IStatechartEvent _authenticationFailEvent = new StatechartEvent("Authentication Fail Event");
		private readonly IStatechartEvent _authenticationRegisterFailEvent = new StatechartEvent("Authentication Register Fail Event");
		
		private readonly IGameDataProvider _dataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiServiceInit _uiService;
		private readonly IDataService _dataService;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private IConfigsAdder _configsAdder;
		private string _passwordRecoveryEmailTemplateId = "";
		
		public AuthenticationState(IGameDataProvider dataProvider, IGameServices services, IGameUiServiceInit uiService, IDataService dataService, 
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
			var login = stateFactory.State("Login");
			var accountDeleted = stateFactory.State("Account Deleted");
			var guestLogin = stateFactory.State("Guest Login");
			var autoAuthCheck = stateFactory.Choice("Auto Auth Check");
			var accountStateCheck = stateFactory.Choice("Account Deleted");
			var register = stateFactory.State("Register");
			var authLogin = stateFactory.State("Authentication Login");
			var authLoginDevice = stateFactory.State("Login Device Authentication");
			var getServerState = stateFactory.Wait("Get Server State");

			initial.Transition().Target(autoAuthCheck);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(SetAuthenticationData);
			
			autoAuthCheck.Transition().Condition(HasLinkedDevice).Target(authLoginDevice);
			autoAuthCheck.Transition().Condition(() => !FeatureFlags.EMAIL_AUTH).OnTransition(()=> { SetLinkedDevice(true); }).Target(authLoginDevice);
			autoAuthCheck.Transition().OnTransition(CloseLoadingScreen).Target(login);

			login.OnEnter(OpenLoginScreen);
			login.Event(_goToRegisterClickedEvent).OnTransition(CloseLoginScreen).Target(register);
			login.Event(_loginAsGuestEvent).Target(guestLogin);
			login.Event(_loginRegisterTransitionEvent).Target(authLogin);

			guestLogin.OnEnter(() => { DimLoginRegisterScreens(true); SetupGuestAccount(); });
			guestLogin.Event(_loginCompletedEvent).Target(authLoginDevice);
			guestLogin.Event(_authenticationFailEvent).OnTransition(() => {DimLoginRegisterScreens(false);}).Target(login);
			
			register.OnEnter(OpenRegisterScreen);
			register.Event(_goToLoginClickedEvent).OnTransition(CloseRegisterScreen).Target(login);
			register.Event(_loginRegisterTransitionEvent).Target(authLogin);

			authLoginDevice.OnEnter(() => DimLoginRegisterScreens(true));
			authLoginDevice.OnEnter(LoginWithDevice);
			authLoginDevice.Event(_loginCompletedEvent).Target(getServerState);
			authLoginDevice.Event(_authenticationFailEvent).OnTransition(()=>{SetLinkedDevice(false); CloseLoadingScreen();}).Target(login);
			authLoginDevice.OnEnter(() => DimLoginRegisterScreens(false));
			
			authLogin.OnEnter(() => DimLoginRegisterScreens(true));
			authLogin.Event(_loginCompletedEvent).OnTransition(CloseLoginRegisterScreens).Target(getServerState);
			authLogin.Event(_authenticationFailEvent).Target(login);
			authLogin.Event(_authenticationRegisterFailEvent).Target(register);
			authLogin.OnExit(() => DimLoginRegisterScreens(false));
			
			getServerState.OnEnter(OpenLoadingScreen);
			getServerState.WaitingFor(FinalStepsAuthentication).Target(accountStateCheck);

			accountDeleted.OnEnter(AccountDeletedPopup);

			accountStateCheck.Transition().Condition(IsAccountDeleted).Target(accountDeleted);
			accountStateCheck.Transition().Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ServerHttpErrorMessage>(OnConnectionError);
		}

		private void UnsubscribeEvents()
		{
			// TODO: Re-add the unsubscription when we can have global state for the authentication or just the re-login on the connection loss
			//_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		/// <summary>
		/// Create a new account by a random customID
		/// And links the current device to that account
		/// </summary>
		private void SetupGuestAccount()
		{
			FLog.Verbose($"Creating guest account");
			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = true,
				CustomId = Guid.NewGuid().ToString(),
			};
			PlayFabClientAPI.LoginWithCustomID(login, res =>
			{
				FLog.Verbose($"Created guest account {res.PlayFabId} linking device");
				_services.PlayfabService.LinkDeviceID(() =>
				{
					FLog.Verbose("Device linked to new account");
					SetLinkedDevice(true);
					CloseLoginScreen();
					_statechartTrigger(_loginCompletedEvent);
				});
			},OnAuthenticationFail);
		}

		private void OnConnectionError(ServerHttpErrorMessage msg)
		{
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Session, "Invalid Session Ticket:"+msg.Message );
			
			if (msg.ErrorCode != HttpStatusCode.Unauthorized)
			{
				throw new PlayFabException(PlayFabExceptionCode.AuthContextRequired, msg.Message);
			}
			
			LoginWithDevice();
			
			_services.PlayfabService.CallFunction("GetPlayerData", res => 
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
				Callback = () =>
				{
					_services.QuitGame("Closing playfab critical error alert");
				},
				Style = AlertButtonStyle.Negative,
				Text = ScriptLocalization.MainMenu.QuitGameButton
			};

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.MainMenu.PlayfabError, error.ErrorMessage, button);
		}
		
		private void OnPlayFabError(PlayFabError error) 
		{
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Login, error.ErrorMessage);
			
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error("Authentication Fail - " + JsonConvert.SerializeObject(error.ErrorDetails));
			}
			
			_services.GenericDialogService.OpenDialog(error.ErrorMessage, false, confirmButton);
			
			DimLoginRegisterScreens(false);
		}
		
		private void OnAuthenticationFail(PlayFabError error)
		{
			OnPlayFabError(error);
			_statechartTrigger(_authenticationFailEvent);
		}
		
		private void OnAuthenticationRegisterFail(PlayFabError error)
		{
			OnPlayFabError(error);
			_statechartTrigger(_authenticationRegisterFailEvent);
		}
		
		private void OnAutomaticAuthenticationFail(PlayFabError error)
		{
			_dataService.GetData<AppData>().DeviceId = null;
			_dataService.SaveData<AppData>();
			_statechartTrigger(_authenticationFailEvent);
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Login, "AutomaticLogin:"+error.ErrorMessage);
		}

		private bool HasLinkedDevice()
		{
			return !string.IsNullOrWhiteSpace(_dataService.GetData<AppData>().DeviceId);
		}

		public void LoginWithDevice()
		{
			FLog.Verbose("Logging in with device ID");
			var deviceId = _dataService.GetData<AppData>().DeviceId;
			var infoParams = new GetPlayerCombinedInfoRequestParams
			{
				GetUserAccountInfo = true,
				GetTitleData = true
			};

#if UNITY_EDITOR
			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = false,
				CustomId = deviceId,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithCustomID(login, OnLoginSuccess, OnAutomaticAuthenticationFail);
			
#elif UNITY_ANDROID
			var login = new LoginWithAndroidDeviceIDRequest()
			{
				CreateAccount = false,
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = deviceId,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithAndroidDeviceID(login, OnLoginSuccess, OnAutomaticAuthenticationFail);
#elif UNITY_IOS
			var login = new LoginWithIOSDeviceIDRequest()
			{
				CreateAccount = false,
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = deviceId,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithIOSDeviceID(login, OnLoginSuccess, OnAutomaticAuthenticationFail);
#endif
		}

		private void SetAuthenticationData()
		{
			var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;
			var appData = _dataService.GetData<AppData>();
			var environment = "";
#if LIVE_SERVER
			environment = "live";
			PlayFabSettings.TitleId = "***REMOVED***";
			quantumSettings.AppSettings.AppIdRealtime = "***REMOVED***";
			_passwordRecoveryEmailTemplateId = "***REMOVED***";
#elif LIVE_TESTNET_SERVER
			environment = "live testnet";
			PlayFabSettings.TitleId = "***REMOVED***";
			quantumSettings.AppSettings.AppIdRealtime = "81262db7-24a2-4685-b386-65427c73ce9d";
			_passwordRecoveryEmailTemplateId = "***REMOVED***";
#elif STAGE_SERVER
			environment = "stage";
			PlayFabSettings.TitleId = "***REMOVED***";
			quantumSettings.AppSettings.AppIdRealtime = "***REMOVED***";
			_passwordRecoveryEmailTemplateId = "***REMOVED***";
#else
			PlayFabSettings.TitleId = "***REMOVED***";
			_passwordRecoveryEmailTemplateId = "***REMOVED***";
			quantumSettings.AppSettings.AppIdRealtime = "***REMOVED***";
#endif
			
			if (environment != appData.Environment)
			{
				var newData = appData.Copy();

				newData.Environment = environment;
				
				_dataService.AddData(newData, true);
				_dataService.SaveData<AppData>();
			}
		}

		private void ProcessAuthentication(LoginResult result)
		{
			var titleData = result.InfoResultPayload.TitleData;
			var appData = _dataService.GetData<AppData>();

			PlayFabSettings.staticPlayer.CopyFrom(result.AuthenticationContext);
			_services.AnalyticsService.SessionCalls.PlayerLogin(result.PlayFabId);
			FLog.Verbose($"Logged in. PlayfabId={result.PlayFabId}");
			//AppleApprovalHack(result);
			
			if(!titleData.TryGetValue(nameof(Application.version), out var titleVersion))
			{
				throw new Exception($"{nameof(Application.version)} not set in title data");
			}
				
			if (IsOutdated(titleVersion))
			{
				OpenGameUpdateDialog(titleVersion);
				return;
			}

			if (titleData.TryGetValue($"{nameof(Application.version)} block", out var version) && IsOutdated(version))
			{
				OpenGameBlockedDialog();
				return;
			}
			
			FeatureFlags.ParseFlags(titleData);
			
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
				FLog.Verbose($"Updating config from version {_configsAdder.Version} to {remoteConfig.Version}");
				_services.MessageBrokerService.Publish(new ConfigurationUpdate()
				{
					NewConfig = remoteConfig,
					OldConfig = _configsAdder
				});
				_configsAdder.UpdateTo(remoteConfig.Version, remoteConfig.GetAllConfigs());
			}

			_dataService.SaveData<AppData>();
			FLog.Verbose("Saved AppData");
		}

		private void FinalStepsAuthentication(IWaitActivity activity)
		{
			FLog.Verbose("Obtaining player data");
			_services.PlayfabService.CallFunction("GetPlayerData", res => OnPlayerDataObtained(res, activity), 
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
				_networkService.QuantumClient.AuthValues.AddAuthParameter("token", result.PhotonCustomAuthenticationToken);
				activity.Complete();
			}
		}

		private void AccountDeletedPopup()
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Confirm,
				ButtonOnClick = () =>
				{
					_services.QuitGame("Deleted User");
				}
			};
			_services.GenericDialogService.OpenDialog(ScriptLocalization.MainMenu.DeleteAccountConfirm, false, confirmButton);
		}

		private bool IsAccountDeleted()
		{
			var playerData = _dataService.GetData<PlayerData>();
			if (playerData.Flags.HasFlag(PlayerFlags.Deleted))
			{
				return true;
			}
			return false;
		}

		private void OnPlayerDataObtained(ExecuteFunctionResult res, IWaitActivity activity)
		{
			var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(res.FunctionResult.ToString());
			var data = serverResult.Result.Data;
			_dataService.AddData(ModelSerializer.DeserializeFromData<RngData>(data));
			_dataService.AddData(ModelSerializer.DeserializeFromData<IdData>(data));
			_dataService.AddData(ModelSerializer.DeserializeFromData<PlayerData>(data));
			_dataService.AddData(ModelSerializer.DeserializeFromData<EquipmentData>(data));
			FLog.Verbose("Downloaded state from server");
			activity?.Complete();
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
				Callback = () =>
				{
					_services.QuitGame("Closing game blocked dialog");
				}
			};

			var message = string.Format(ScriptLocalization.General.MaintenanceDescription,
			                            VersionUtils.VersionExternal);

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.Maintenance, message, confirmButton);
		}
		
		private void LoginClicked(string email, string password)
		{
			_statechartTrigger(_loginRegisterTransitionEvent);
			
			var infoParams = new GetPlayerCombinedInfoRequestParams
			{
				GetUserAccountInfo = true,
				GetTitleData = true
			};
			
			var login = new LoginWithEmailAddressRequest
			{
				Email = email,
				Password = password,
				InfoRequestParameters = infoParams
			};

			PlayFabClientAPI.LoginWithEmailAddress(login, OnLoginSuccess, OnAuthenticationFail);
		}

		private void OnLoginSuccess(LoginResult result)
		{
			var appData = _dataService.GetData<AppData>();
			var userId = result.PlayFabId;
			var email = result.InfoResultPayload.AccountInfo.PrivateInfo.Email;
			var userName = result.InfoResultPayload.AccountInfo.Username;

			_services.HelpdeskService.Login(userId, email, userName);

			if (string.IsNullOrWhiteSpace(appData.DeviceId))
			{
				_services.PlayfabService.LinkDeviceID(null, null);
			}

			ProcessAuthentication(result);

			_statechartTrigger(_loginCompletedEvent);
		}

		private void RegisterClicked(string email, string username, string password)
		{
			_statechartTrigger(_loginRegisterTransitionEvent);
			
			var register = new RegisterPlayFabUserRequest
			{
				Email = email,
				DisplayName = username,
				Username = username,
				Password = password
			};

			PlayFabClientAPI.RegisterPlayFabUser(register, _ => LoginClicked(email, password), OnAuthenticationRegisterFail);
		}
		
		private void OpenLoadingScreen()
		{
			_uiService.OpenUi<LoadingScreenPresenter>();
		}

		private void CloseLoginScreen()
		{
			_uiService.CloseUi<LoginScreenPresenter>();
		}
		
		private void CloseLoadingScreen()
		{
			_uiService.CloseUi<LoadingScreenPresenter>();
		}

		private void CloseRegisterScreen()
		{
			_uiService.CloseUi<RegisterScreenPresenter>();
		}
		
		private void CloseLoginRegisterScreens()
		{
			_uiService.CloseUi<LoginScreenPresenter>();
			_uiService.CloseUi<RegisterScreenPresenter>();
		}

		private void DimLoginRegisterScreens(bool dimmed)
		{
			if (_uiService.HasUiPresenter<LoginScreenPresenter>())
			{
				_uiService.GetUi<LoginScreenPresenter>().SetFrontDimBlockerActive(dimmed);
			}

			if (_uiService.HasUiPresenter<RegisterScreenPresenter>())
			{
				_uiService.GetUi<RegisterScreenPresenter>().SetFrontDimBlockerActive(dimmed);
			}
		}

		private void OpenLoginScreen()
		{
			var data = new LoginScreenPresenter.StateData
			{
				LoginClicked = LoginClicked,
				GoToRegisterClicked = () => _statechartTrigger(_goToRegisterClickedEvent),
				PlayAsGuestClicked = () =>
				{
					DimLoginRegisterScreens(true);
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
			SendAccountRecoveryEmailRequest request = new SendAccountRecoveryEmailRequest()
			{
				TitleId = PlayFabSettings.TitleId,
				Email = email,
				EmailTemplateId = _passwordRecoveryEmailTemplateId,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};
			
			PlayFabClientAPI.SendAccountRecoveryEmail(request,OnAccountRecoveryResult,OnPlayFabError);
		}
		
		private void OnAccountRecoveryResult(SendAccountRecoveryEmailResult result)
		{
			_services.GenericDialogService.CloseDialog();
			
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			_services.GenericDialogService.OpenDialog(ScriptLocalization.MainMenu.SendPasswordEmailConfirm, false,
			                                         confirmButton);
		}
		
		private void SetLinkedDevice(bool linked)
		{
			_dataProvider.AppDataProvider.DeviceID.Value = linked ? PlayFabSettings.DeviceUniqueIdentifier : "";
			_dataService.SaveData<AppData>();
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
	}
}