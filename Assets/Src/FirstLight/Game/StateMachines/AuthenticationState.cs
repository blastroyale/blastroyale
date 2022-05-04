using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
		private readonly IStatechartEvent _authenticationFailEvent = new StatechartEvent("Authentication Fail Event");
		
		private readonly GameLogic _gameLogic;
		private readonly IGameServices _services;
		private readonly IGameUiServiceInit _uiService;
		private readonly IDataService _dataService;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		
		private string _selectedAuthEmail;
		private string _selectedAuthName;
		private string _selectedAuthPass;
		
		public AuthenticationState(GameLogic gameLogic, IGameServices services, IGameUiServiceInit uiService, 
		                           IDataService dataService, IGameBackendNetworkService networkService, 
		                           Action<IStatechartEvent> statechartTrigger)
		{
			_gameLogic = gameLogic;
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
			var authLoginEmail = stateFactory.Wait("Login Email Authentication");
			var authLoginDevice = stateFactory.Wait("Login Device Authentication");
			var authRegister = stateFactory.Wait("Register Authentication");
			var photonAuthentication = stateFactory.Wait("PlayFab Photon Authentication");

			initial.Transition().Target(autoAuthCheck);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(SetQuantumSettings);
			initial.OnExit(LoadAppData);
			
			autoAuthCheck.Transition().Condition(HasCachedLoginEmail).Target(authLoginDevice);
			autoAuthCheck.Transition().OnTransition(CloseLoadingScreen).Target(login);

			login.OnEnter(OpenLoginScreen);
			login.Event(_goToRegisterClickedEvent).OnTransition(CloseLoginScreen).Target(register);
			login.Event(_loginRegisterTransitionEvent).Target(authLoginEmail);
			
			register.OnEnter(OpenRegisterScreen);
			register.Event(_goToLoginClickedEvent).OnTransition(CloseRegisterScreen).Target(login);
			register.Event(_loginRegisterTransitionEvent).Target(authRegister);

			authLoginDevice.WaitingFor(LoginWithDevice).Target(photonAuthentication);
			authLoginDevice.Event(_authenticationFailEvent).OnTransition(CloseLoadingScreen).Target(login);
			
			authLoginEmail.OnEnter(()=>{ DimLoginScreen(true);});
			authLoginEmail.WaitingFor(LoginWithEmail).OnTransition(OnEmailLoginTransition).Target(photonAuthentication);
			authLoginEmail.Event(_authenticationFailEvent).Target(login);
			authLoginEmail.OnExit(()=>{ DimLoginScreen(false);});
			
			authRegister.OnEnter(()=>{ DimRegisterScreen(true);});
			authRegister.WaitingFor(AuthenticateRegister).OnTransition(CloseRegisterScreen).Target(login);
			authRegister.Event(_authenticationFailEvent).Target(register);
			authRegister.OnExit(()=>{ DimRegisterScreen(false);});
			
			photonAuthentication.WaitingFor(PhotonAuthentication).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}
		/// <summary>
		/// Callback for game-stopping errors. Prompts user to close the game.
		/// </summary>
		public void OnCriticalPlayFabError(PlayFabError error) 
		{
			_services.AnalyticsService.CrashLog(error.ErrorMessage);

			var button = new AlertButton
			{
				Callback = Application.Quit,
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

			_services.GenericDialogService.OpenDialog(error.ErrorMessage, false, confirmButton);
		}
		
		private void OnAuthenticationFail(PlayFabError error, IWaitActivity activity)
		{
			_statechartTrigger(_authenticationFailEvent);
			OnPlayFabError(error);
			activity.Complete();
		}

		private void SubscribeEvents()
		{
			// Subscribe to events
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void LoadAppData()
		{
			_dataService.LoadData<AppData>();
		}

		private void OnEmailLoginTransition()
		{
			CloseLoginScreen(); 
			OpenLoadingScreen();
		}

		private bool HasCachedLoginEmail()
		{
			return !string.IsNullOrEmpty(_dataService.GetData<AppData>().LastLoginEmail);
		}

		private void AuthenticateRegister(IWaitActivity activity)
		{
			var cacheActivity = activity;
			
			var register = new RegisterPlayFabUserRequest
			{
				Email = _selectedAuthEmail,
				Username = _selectedAuthName,
				Password = _selectedAuthPass
			};
			
			PlayFabClientAPI.RegisterPlayFabUser(register, OnRegisterSuccess, (error => {OnAuthenticationFail(error, activity); }));

			void OnRegisterSuccess(RegisterPlayFabUserResult result)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = ()=>{ cacheActivity.Complete(); }
				};
				
				_services.GenericDialogService.OpenDialog(ScriptLocalization.MainMenu.RegisterSuccess,false, confirmButton);
			}
		}

		private void LoginWithEmail(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var infoParams = new GetPlayerCombinedInfoRequestParams
			{
				GetUserAccountInfo = true,
				GetUserReadOnlyData = true,
				GetTitleData = true
			};
			
			var login = new LoginWithEmailAddressRequest()
			{
				Email = _selectedAuthEmail,
				Password = _selectedAuthPass,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithEmailAddress(login, OnLoginSuccess, (error => { OnAuthenticationFail(error, activity); }));

			void OnLoginSuccess(LoginResult result)
			{
				_dataService.GetData<AppData>().LastLoginEmail = _selectedAuthEmail;
				ProcessAuthentication(result, cacheActivity);
			}
		}

		private void LoginWithDevice(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var infoParams = new GetPlayerCombinedInfoRequestParams
			{
				GetUserAccountInfo = true,
				GetUserReadOnlyData = true,
				GetTitleData = true
			};

#if UNITY_EDITOR
			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = true,
				CustomId = PlayFabSettings.DeviceUniqueIdentifier,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithCustomID(login, OnLoginSuccess, (error => { OnAuthenticationFail(error, activity); }));
			
#elif UNITY_ANDROID
			var login = new LoginWithAndroidDeviceIDRequest()
			{
				CreateAccount = true,
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithAndroidDeviceID(login, OnLoginSuccess, OnPlayFabError);
#elif UNITY_IOS
			var login = new LoginWithIOSDeviceIDRequest()
			{
				CreateAccount = true,
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithIOSDeviceID(login, OnLoginSuccess, OnPlayFabError);
#endif

			void OnLoginSuccess(LoginResult result)
			{
				ProcessAuthentication(result, cacheActivity);
			}
		}

		private void SetQuantumSettings()
		{
			var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;
			
#if RELEASE_BUILD
			PlayFabSettings.TitleId = "***REMOVED***";
			quantumSettings.AppSettings.AppIdRealtime = "81262db7-24a2-4685-b386-65427c73ce9d";
#else
			//Config for "Dev_Backend"
			PlayFabSettings.TitleId = "***REMOVED***";
			quantumSettings.AppSettings.AppIdRealtime = "***REMOVED***";
#endif
		}

		private void ProcessAuthentication(LoginResult result, IWaitActivity activity)
		{
			if (!_dataService.GetData<AppData>().LinkedDevice)
			{
				LinkDeviceID(activity.Split());
			}
			
			var titleData = result.InfoResultPayload.TitleData;
			
			PlayFabSettings.staticPlayer.CopyFrom(result.AuthenticationContext);
			_services.AnalyticsService.LoginEvent(result.PlayFabId);
			InitializeGameData(result, activity.Split());
			//AppleApprovalHack(result);

			if (IsOutdated(titleData[nameof(Application.version)]))
			{
				OpenGameUpdateDialog();
				return;
			}
			
			if (titleData.TryGetValue($"{nameof(Application.version)} block", out var version) && IsOutdated(version))
			{
				OpenGameBlockedDialog();
				return;
			}

			activity.Complete();
		}

		private void LinkDeviceID(IWaitActivity activity)
		{
#if UNITY_EDITOR
			var link = new LinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkCustomID(link, OnLinkSuccess, OnLinkFail);

			void OnLinkSuccess(LinkCustomIDResult result)
			{
				_dataService.GetData<AppData>().LinkedDevice = true;
				activity.Complete();
			}

#elif UNITY_ANDROID
			var link = new LinkAndroidDeviceIDRequest
			{
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkAndroidDeviceID(link, OnLinkSuccess, OnLinkFail);
			
			void OnLinkSuccess(LinkAndroidDeviceIDResult result)
			{
				_dataService.GetData<AppData>().LinkedDevice = true;
				_dataService.SaveData<AuthenticationSaveData>();
				activity.Complete();
			}

#elif UNITY_IOS
			var link = new LinkIOSDeviceIDRequest
			{
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkIOSDeviceID(link, OnLinkSuccess, OnLinkFail);
			
			void OnLinkSuccess(LinkIOSDeviceIDResult result)
			{
				_dataService.GetData<AppData>().LinkedDevice = true;
				_dataService.SaveData<AuthenticationSaveData>();
				activity.Complete();
			}
#endif
			
			void OnLinkFail(PlayFabError error)
			{
				OnPlayFabError(error);
				activity.Complete();
			}
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

		private void SetupFirstTimePlayer(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var request = new ExecuteFunctionRequest
			{
				FunctionName = "SetupPlayerCommand",
				GeneratePlayStreamEvent = true,
				AuthenticationContext = PlayFabSettings.staticPlayer,
				FunctionParameter = new LogicRequest
				{
					Command = "SetupPlayerCommand",
					Platform = Application.platform.ToString(),
					Data = new Dictionary<string, string>()
				}
			};
			
			PlayFabCloudScriptAPI.ExecuteFunction(request, OnPlayerSetup, OnCriticalPlayFabError);
			
			void OnPlayerSetup(ExecuteFunctionResult result)
			{
				var logicResult = JsonConvert.DeserializeObject<PlayFabResult<LogicResult>>(result.FunctionResult.ToString());

				_dataService.AddData(ModelSerializer.DeserializeFromData<RngData>(logicResult.Result.Data));
				_dataService.AddData(ModelSerializer.DeserializeFromData<IdData>(logicResult.Result.Data));
				_dataService.AddData(ModelSerializer.DeserializeFromData<PlayerData>(logicResult.Result.Data));

				cacheActivity.Complete();
			}
		}

		private void InitializeGameData(LoginResult result, IWaitActivity activity)
		{
			var data = result.InfoResultPayload.UserReadOnlyData;
			var appData = _dataService.GetData<AppData>();
			
			_networkService.UserId.Value = result.PlayFabId;
			appData.NickNameId = result.InfoResultPayload.AccountInfo.TitleInfo.DisplayName;
			appData.FirstLoginTime = result.InfoResultPayload.AccountInfo.Created;
			appData.LoginTime = _services.TimeService.DateTimeUtcNow;
			appData.LastLoginTime = result.LastLoginTime ?? result.InfoResultPayload.AccountInfo.Created;
			appData.IsFirstSession = result.NewlyCreated;
			
			if (result.NewlyCreated || data.Count == 0)
			{
				SetupFirstTimePlayer(activity.Split());
			}
			else
			{
				_dataService.AddData(ModelSerializer.DeserializeFromData<RngData>(data));
				_dataService.AddData(ModelSerializer.DeserializeFromData<IdData>(data));
				_dataService.AddData(ModelSerializer.DeserializeFromData<PlayerData>(data));
			}

			activity.Complete();
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
				Application.OpenURL(GameConstants.APP_STORE_IOS_LINK);
#elif UNITY_ANDROID
				Application.OpenURL(GameConstants.APP_STORE_GOOGLE_PLAY_LINK);
#endif
			}
		}

		private void OpenGameBlockedDialog()
		{
			var confirmButton = new AlertButton
			{
				Text = ScriptLocalization.General.Confirm,
				Style = AlertButtonStyle.Default,
				Callback = Application.Quit
			};

			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.Maintenance, 
			                               ScriptLocalization.General.MaintenanceDescription, confirmButton);
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
		
		private void LoginClicked(string email, string password)
		{
			_selectedAuthEmail = email;
			_selectedAuthPass = password;
			_statechartTrigger(_loginRegisterTransitionEvent);
		}

		private void RegisterClicked(string email, string username, string password)
		{
			_selectedAuthEmail = email;
			_selectedAuthName = username;
			_selectedAuthPass = password;
			_statechartTrigger(_loginRegisterTransitionEvent);
		}

		private void GoToRegisterClicked()
		{
			_statechartTrigger(_goToRegisterClickedEvent);
		}

		private void GoToLoginClicked()
		{
			_statechartTrigger(_goToLoginClickedEvent);
		}
		
		private void OpenLoadingScreen()
		{
			_uiService.OpenUi<LoadingScreenPresenter>();
		}
		
		private void CloseLoadingScreen()
		{
			_uiService.CloseUi<LoadingScreenPresenter>();
		}

		private void OpenLoginScreen()
		{
			var data = new LoginScreenPresenter.StateData
			{
				LoginClicked = LoginClicked,
				GoToRegisterClicked = GoToRegisterClicked
			};
			
			_uiService.OpenUi<LoginScreenPresenter, LoginScreenPresenter.StateData>(data);
		}

		private void CloseLoginScreen()
		{
			_uiService.CloseUi<LoginScreenPresenter>();
		}

		private void OpenRegisterScreen()
		{
			var data = new RegisterScreenPresenter.StateData
			{
				RegisterClicked = RegisterClicked,
				GoToLoginClicked = GoToLoginClicked
			};
			
			_uiService.OpenUi<RegisterScreenPresenter, RegisterScreenPresenter.StateData>(data);
		}

		private void CloseRegisterScreen()
		{
			_uiService.CloseUi<RegisterScreenPresenter>();
		}

		private void DimRegisterScreen(bool dimmed)
		{
			_uiService.GetUi<RegisterScreenPresenter>().SetFrontDimBlockerActive(dimmed);
		}

		private void DimLoginScreen(bool dimmed)
		{
			_uiService.GetUi<LoginScreenPresenter>().SetFrontDimBlockerActive(dimmed);
		}

		private bool IsOutdated(string version)
		{
			var appVersion = Application.version.Split('.');
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
	}
}