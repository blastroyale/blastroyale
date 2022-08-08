using System;
using System.Net;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
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
		private readonly IStatechartEvent _goToLoginClickedEvent = new StatechartEvent("Go To Login Clicked Event");
		private readonly IStatechartEvent _loginRegisterTransitionEvent = new StatechartEvent("Login Register Transition Clicked Event");
		private readonly IStatechartEvent _loginCompletedEvent = new StatechartEvent("Login Completed Event");
		private readonly IStatechartEvent _authenticationFailEvent = new StatechartEvent("Authentication Fail Event");
		
		private readonly IGameServices _services;
		private readonly IGameUiServiceInit _uiService;
		private readonly IDataService _dataService;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		
		public AuthenticationState(IGameServices services, IGameUiServiceInit uiService, IDataService dataService, 
		                           IGameBackendNetworkService networkService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_dataService = dataService;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Initial Loading state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var login = stateFactory.State("Login");
			var autoAuthCheck = stateFactory.Choice("Auto Auth Check");
			var register = stateFactory.State("Register");
			var authLogin = stateFactory.State("Authentication Login");
			var authLoginDevice = stateFactory.State("Login Device Authentication");
			var getServerState = stateFactory.Wait("Get Server State");

			initial.Transition().Target(autoAuthCheck);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(SetAuthenticationData);
			
			autoAuthCheck.Transition().Condition(HasLinkedDevice).Target(authLoginDevice);
			autoAuthCheck.Transition().OnTransition(CloseLoadingScreen).Target(login);

			login.OnEnter(OpenLoginScreen);
			login.Event(_goToRegisterClickedEvent).OnTransition(CloseLoginScreen).Target(register);
			login.Event(_loginRegisterTransitionEvent).Target(authLogin);

			register.OnEnter(OpenRegisterScreen);
			register.Event(_goToLoginClickedEvent).OnTransition(CloseRegisterScreen).Target(login);
			register.Event(_loginRegisterTransitionEvent).Target(authLogin);

			authLoginDevice.OnEnter(LoginWithDevice);
			authLoginDevice.Event(_loginCompletedEvent).Target(getServerState);
			authLoginDevice.Event(_authenticationFailEvent).OnTransition(CloseLoadingScreen).Target(login);
			
			authLogin.OnEnter(() => DimLoginRegisterScreens(true));
			authLogin.Event(_loginCompletedEvent).OnTransition(CloseLoginRegisterScreens).Target(getServerState);
			authLogin.Event(_authenticationFailEvent).Target(login);
			authLogin.OnExit(() => DimLoginRegisterScreens(false));
			
			getServerState.OnEnter(OpenLoadingScreen);
			getServerState.WaitingFor(FinalStepsAuthentication).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ServerHttpError>(OnConnectionError);
		}

		private void UnsubscribeEvents()
		{
			// TODO: Re-add the unsubscription when we can have global state for the authentication or just the re-login on the connection loss
			//_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnConnectionError(ServerHttpError msg)
		{
			if (msg.ErrorCode != HttpStatusCode.Unauthorized)
			{
				throw new PlayFabException(PlayFabExceptionCode.AuthContextRequired, msg.Message);
			}

			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Session, "Invalid Session Ticket");
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
					_services.GameFlowService.QuitGame("Closing playfab critical error alert");
				},
				Style = AlertButtonStyle.Negative,
				Text = ScriptLocalization.MainMenu.QuitGameButton
			};

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.MainMenu.PlayfabError, error.ErrorMessage, button);
		}
		
		private void OnPlayFabError(PlayFabError error) 
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error(JsonConvert.SerializeObject(error.ErrorDetails));
			}
			_services.GenericDialogService.OpenDialog(error.ErrorMessage, false, confirmButton);
		}
		
		private void OnAuthenticationFail(PlayFabError error)
		{
			_statechartTrigger(_authenticationFailEvent);
			OnPlayFabError(error);
		}

		private bool HasLinkedDevice()
		{
			return !FeatureFlags.EMAIL_AUTH || _dataService.GetData<AppData>().LinkedDevice;
		}

		private void LoginWithDevice()
		{
			FLog.Verbose("Logging in with device ID");
			var infoParams = new GetPlayerCombinedInfoRequestParams
			{
				GetUserAccountInfo = true,
				GetTitleData = true
			};

#if UNITY_EDITOR
			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = true,
				CustomId = PlayFabSettings.DeviceUniqueIdentifier,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithCustomID(login, OnLoginSuccess, OnAuthenticationFail);
			
#elif UNITY_ANDROID
			var login = new LoginWithAndroidDeviceIDRequest()
			{
				CreateAccount = true,
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithAndroidDeviceID(login, OnLoginSuccess, OnAuthenticationFail);
#elif UNITY_IOS
			var login = new LoginWithIOSDeviceIDRequest()
			{
				CreateAccount = true,
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithIOSDeviceID(login, OnLoginSuccess, OnAuthenticationFail);
#endif
		}

		private void SetAuthenticationData()
		{
			var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;

#if STORE_BUILD
			if (!FeatureFlags.TEMP_PRODUCTION_PLAYFAB)
			{
				PlayFabSettings.TitleId = "***REMOVED***";
				quantumSettings.AppSettings.AppIdRealtime = "81262db7-24a2-4685-b386-65427c73ce9d";
			} 
			else 
			{
				PlayFabSettings.TitleId = "302CF";
				quantumSettings.AppSettings.AppIdRealtime = "***REMOVED***";
			}
#elif RELEASE_BUILD
			// Staging
			PlayFabSettings.TitleId = "***REMOVED***";
			quantumSettings.AppSettings.AppIdRealtime = "***REMOVED***";
#else
			// Dev
			PlayFabSettings.TitleId = "***REMOVED***";
			quantumSettings.AppSettings.AppIdRealtime = "***REMOVED***";
#endif
			_dataService.LoadData<AppData>();
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
				OpenGameUpdateDialog();
				return;
			}

			if (titleData.TryGetValue($"{nameof(Application.version)} block", out var version) && IsOutdated(version))
			{
				OpenGameBlockedDialog();
				return;
			}

			_networkService.UserId.Value = result.PlayFabId;
			appData.NickNameId = result.InfoResultPayload.AccountInfo.TitleInfo.DisplayName;
			appData.FirstLoginTime = result.InfoResultPayload.AccountInfo.Created;
			appData.LoginTime = _services.TimeService.DateTimeUtcNow;
			appData.LastLoginTime = result.LastLoginTime ?? result.InfoResultPayload.AccountInfo.Created;
			appData.IsFirstSession = result.NewlyCreated;
			appData.PlayerId = result.PlayFabId;

			_dataService.SaveData<AppData>();
			FLog.Verbose("Saved AppData");
		}

		private void LinkDeviceID()
		{
#if UNITY_EDITOR
			var link = new LinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkCustomID(link, _ => OnLinkSuccess(), OnLinkFail);
#elif UNITY_ANDROID
			var link = new LinkAndroidDeviceIDRequest
			{
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkAndroidDeviceID(link, _ => OnLinkSuccess(), OnLinkFail);

#elif UNITY_IOS
			var link = new LinkIOSDeviceIDRequest
			{
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkIOSDeviceID(link, _ => OnLinkSuccess(), OnLinkFail);
#endif
			
			void OnLinkFail(PlayFabError error)
			{
				OnPlayFabError(error);
			}
			
			void OnLinkSuccess()
			{
				_dataService.GetData<AppData>().LinkedDevice = true;
				_dataService.SaveData<AppData>();
				FLog.Verbose("Linked account with device in playfab");
			}
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

		private void OpenGameUpdateDialog()
		{
			var confirmButton = new AlertButton
			{
				Text = ScriptLocalization.General.Confirm,
				Style = AlertButtonStyle.Positive,
				Callback = OpenStore
			};

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.NewGameUpdate, 
			                               ScriptLocalization.General.UpdateGame, confirmButton);

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
					_services.GameFlowService.QuitGame("Closing game blocked dialog");
				}
			};

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.Maintenance, 
			                               ScriptLocalization.General.MaintenanceDescription, confirmButton);
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
			
			if (!appData.LinkedDevice)
			{
				LinkDeviceID();
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
			
			PlayFabClientAPI.RegisterPlayFabUser(register, _ => LoginClicked(email, password), OnAuthenticationFail);
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
				GoToRegisterClicked = () => _statechartTrigger(_goToRegisterClickedEvent)
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