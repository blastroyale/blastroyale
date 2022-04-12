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
		private readonly GameLogic _gameLogic;
		private readonly IGameServices _services;
		private readonly IGameUiServiceInit _uiService;
		private readonly IDataService _dataService;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		
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
			var authentication = stateFactory.Wait("Authentication");
			var photonAuthentication = stateFactory.Wait("PlayFab Photon Authentication");

			initial.Transition().Target(authentication);
			initial.OnExit(SubscribeEvents);
			
			authentication.OnEnter(SetQuantumSettings);
			authentication.WaitingFor(Authenticate).Target(photonAuthentication);
			
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

		private void Authenticate(IWaitActivity activity)
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
			
			PlayFabClientAPI.LoginWithCustomID(login, OnLoginSuccess, OnPlayFabError);
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
				var converter = new StringEnumConverter();
				
				_dataService.AddData(JsonConvert.DeserializeObject<RngData>(logicResult.Result.Data[nameof(RngData)], converter));
				_dataService.AddData(JsonConvert.DeserializeObject<IdData>(logicResult.Result.Data[nameof(IdData)], converter));
				_dataService.AddData(JsonConvert.DeserializeObject<PlayerData>(logicResult.Result.Data[nameof(PlayerData)], converter));
				
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
				var converter = new StringEnumConverter();
				
				_dataService.AddData(JsonConvert.DeserializeObject<RngData>(data[nameof(RngData)].Value, converter));
				_dataService.AddData(JsonConvert.DeserializeObject<IdData>(data[nameof(IdData)].Value, converter));
				_dataService.AddData(JsonConvert.DeserializeObject<PlayerData>(data[nameof(PlayerData)].Value, converter));
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

		// To help pass apple approval submission tests hack
		private async void AppleApprovalHack(LoginResult result)
		{
			var address = AddressableId.Configs_Settings_QuantumRunnerConfigs.GetConfig().Address;
			var config = await _services.AssetResolverService.LoadAssetAsync<QuantumRunnerConfigs>(address);
			
			config.PhotonServerSettings.AppSettings.Protocol = result.InfoResultPayload.TitleData.ContainsKey("connection")
				                                                   ? ConnectionProtocol.Tcp
				                                                   : ConnectionProtocol.Udp;
			
			_services.AssetResolverService.UnloadAsset(config);
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