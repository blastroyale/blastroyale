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
		private readonly IStatechartEvent _loginClickedEvent = new StatechartEvent("Login Clicked Event");
		private readonly IStatechartEvent _registerClickedEvent = new StatechartEvent("Register Clicked Event");
		private readonly IStatechartEvent _authenticationSuccessEvent = new StatechartEvent("Authentication Success Event");
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
		/// Callback to be used during PlayFab initial authentication proccess
		/// </summary>
		/// <param name="error"></param>
		public void OnPlayFabError(PlayFabError error) 
		{
			_services.AnalyticsService.CrashLog(error.ErrorMessage);

			var button = new AlertButton
			{
				Callback = Application.Quit,
				Style = AlertButtonStyle.Negative,
				Text = "Quit Game"
			};

			NativeUiService.ShowAlertPopUp(false, "Game Error", error.ErrorMessage, button);
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
			
			autoAuthCheck.Transition().Condition(HasCachedLoginEmail).Target(authLoginDevice);
			autoAuthCheck.Transition().OnTransition(CloseLoadingScreen).Target(login);
			
			login.OnEnter(OpenLoginScreen);
			login.Event(_goToRegisterClickedEvent).Target(register);
			login.Event(_loginClickedEvent).Target(authLoginEmail);
			login.OnExit(CloseLoginScreen);
			
			register.OnEnter(OpenRegisterScreen);
			register.Event(_goToLoginClickedEvent).OnTransition(CloseRegisterScreen).Target(login);
			register.Event(_registerClickedEvent).Target(authRegister);
			register.OnExit(CloseRegisterScreen);
			
			authLoginDevice.WaitingFor(LoginWithDevice).Target(photonAuthentication);
			authLoginDevice.Event(_authenticationFailEvent).Target(login);
			
			authLoginEmail.WaitingFor(LoginWithEmail).Target(photonAuthentication);
			authLoginEmail.Event(_authenticationFailEvent).Target(login);
			
			authRegister.WaitingFor(AuthenticateRegister).Target(login);
			
			photonAuthentication.WaitingFor(PhotonAuthentication).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			// Subscribe to events
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private bool HasCachedLoginEmail()
		{
			var authSaveData = _dataService.LoadData<AuthenticationSaveData>();
			
			if (string.IsNullOrEmpty(authSaveData.LastLoginEmail))
			{
				return false;
			}

			return true;
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
			
			PlayFabClientAPI.RegisterPlayFabUser(register, OnRegisterSuccess, OnRegisterFail);

			void OnRegisterSuccess(RegisterPlayFabUserResult result)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = () => { cacheActivity.Complete(); }
				};
				
				_services.GenericDialogService.OpenDialog(ScriptLocalization.MainMenu.RegisterSuccess,false, confirmButton);
			}

			void OnRegisterFail(PlayFabError error)
			{
				OnPlayFabError(error);
				cacheActivity.Complete();
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
			
			PlayFabClientAPI.LoginWithEmailAddress(login, OnLoginSuccess, OnLoginFail);

			void OnLoginSuccess(LoginResult result)
			{
				var authSaveData = _dataService.GetData<AuthenticationSaveData>();
				authSaveData.LastLoginEmail = _selectedAuthEmail;

				LinkDeviceID(cacheActivity.Split());
				ProcessAuthentication(result, cacheActivity);
			}

			void OnLoginFail(PlayFabError error)
			{
				_statechartTrigger(_authenticationFailEvent);
				OnPlayFabError(error);
				cacheActivity.Complete();
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
			
			PlayFabClientAPI.LoginWithCustomID(login, OnLoginSuccess, OnLoginFail);
			
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

			void OnLoginFail(PlayFabError error)
			{
				_statechartTrigger(_authenticationFailEvent);
				OnPlayFabError(error);
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
				CustomId = PlayFabSettings.DeviceUniqueIdentifier
			};
			
			PlayFabClientAPI.LinkCustomID(link, OnLinkSuccess, OnLinkFail);

			void OnLinkSuccess(LinkCustomIDResult result)
			{
				activity.Complete();
			}

			void OnLinkFail(PlayFabError error)
			{
				OnPlayFabError(error);
				activity.Complete();
			}

			// TODO - LINK PROPERLY ANDROID/IOS
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
				activity.Complete();
			}

			void OnLinkFail(PlayFabError error)
			{
				OnPlayFabError(error);
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
				activity.Complete();
			}

			void OnLinkFail(PlayFabError error)
			{
				OnPlayFabError(error);
				activity.Complete();
			}
#endif
		}

		private void PhotonAuthentication(IWaitActivity activity)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var appId = config.PhotonServerSettings.AppSettings.AppIdRealtime;
			var request = new GetPhotonAuthenticationTokenRequest { PhotonApplicationId = appId };
			
			PlayFabClientAPI.GetPhotonAuthenticationToken(request, OnAuthenticationSuccess, OnPlayFabError);

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
			
			PlayFabCloudScriptAPI.ExecuteFunction(request, OnPlayerSetup, OnPlayFabError);
			
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
			var appData = _dataService.LoadData<AppData>();
			
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
			_statechartTrigger(_loginClickedEvent);
		}

		private void RegisterClicked(string email, string username, string password)
		{
			_selectedAuthEmail = email;
			_selectedAuthName = username;
			_selectedAuthPass = password;
			_statechartTrigger(_registerClickedEvent);
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