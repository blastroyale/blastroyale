using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.SharedModels;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public class LoginData
	{
		public bool IsGuest;
	}

	/// <summary>
	/// This services handles all authentication functionality
	/// </summary>
	public interface IAuthenticationService
	{
		/// <summary>
		/// Creates and authenticates a new account with a random customID (GUID), and links the current device
		/// </summary>
		void LoginSetupGuest(Action<LoginData> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Authenticates the backend with current device as the credentials
		/// </summary>
		void LoginWithDevice(Action<LoginData> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Authenticates the backend with an email address and password
		/// </summary>
		void LoginWithEmail(string email, string password, Action<LoginData> onSuccess, Action<PlayFabError> onError);
	}

	public interface IInternalAuthenticationService : IAuthenticationService
	{
		/// <summary>
		/// Processes authentication response.
		/// This includes setting auth context, parsing feature flags, parsing remote configs, saving relevant local data, etc.
		/// </summary>
		void ProcessAuthentication(LoginResult result, LoginData loginData, Action<LoginData> onSuccess,
								   Action<PlayFabError> onError);

		/// <summary>
		/// Calls request to download the player account data
		/// </summary>
		void GetPlayerData();

		/// <summary>
		/// Deserializes and adds the obtained player state data into data service
		/// </summary>
		void AddDataToService(Dictionary<string, string> data);

		/// <summary>
		/// Authenticates photon with the processed authentication data
		/// </summary>
		void AuthenticatePhoton();

		/// <summary>
		/// Requests to check if a given account is due for deletion
		/// </summary>
		bool IsAccountDeleted(PlayerData playerData);
	}

	/// <inheritdoc cref="IAuthenticationService" />
	public class PlayfabAuthenticationService : IInternalAuthenticationService
	{
		private IGameServices _services;
		private IDataService _dataService;
		private IGameNetworkService _networkService;
		private IGameDataProvider _dataProvider;
		private IConfigsAdder _configsAdder;
		
		public PlayfabAuthenticationService(IGameServices services, IDataService dataService, IGameNetworkService networkService,
											IGameDataProvider dataProvider, IConfigsAdder configsAdder)
		{
			_services = services;
			_dataService = dataService;
			_networkService = networkService;
			_dataProvider = dataProvider;
			_configsAdder = configsAdder;
		}

		public void LoginSetupGuest(Action<LoginData> onSuccess, Action<PlayFabError> onError)
		{
			FLog.Verbose($"Creating guest account");

			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = true,
				CustomId = Guid.NewGuid().ToString(),
			};

			var loginData = new LoginData() {IsGuest = true};

			PlayFabClientAPI.LoginWithCustomID(login,
				(res) => ProcessAuthentication(res, loginData, onSuccess, onError), onError);
		}

		public void LoginWithDevice(Action<LoginData> onSuccess, Action<PlayFabError> onError)
		{
			FLog.Verbose("Logging in with device ID");

			var deviceId = _dataService.GetData<AppData>().DeviceId;
			var infoParams = new GetPlayerCombinedInfoRequestParams
			{
				GetPlayerProfile = true,
				GetUserAccountInfo = true,
				GetTitleData = true
			};

			var loginData = new LoginData() {IsGuest = false};

#if UNITY_EDITOR
			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = false,
				CustomId = deviceId,
				InfoRequestParameters = infoParams
			};

			PlayFabClientAPI.LoginWithCustomID(login,
				(res) => ProcessAuthentication(res, loginData, onSuccess, onError), onError);
#elif UNITY_ANDROID
			var login = new LoginWithAndroidDeviceIDRequest()
			{
				CreateAccount = false,
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = deviceId,
				InfoRequestParameters = infoParams
			};
			
			PlayFabClientAPI.LoginWithAndroidDeviceID(login, (res) => ProcessAuthentication(res, loginData, onSuccess, onError), onError);
#elif UNITY_IOS
			var login = new LoginWithIOSDeviceIDRequest()
			{
				CreateAccount = false,
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = deviceId,
				InfoRequestParameters = infoParams
			};

			PlayFabClientAPI.LoginWithIOSDeviceID(login, (res) => ProcessAuthentication(res, loginData, onSuccess, onError), onError);
#endif
		}

		public void LoginWithEmail(string email, string password, Action<LoginData> onSuccess,
								   Action<PlayFabError> onError)
		{
			var infoParams = new GetPlayerCombinedInfoRequestParams
			{
				GetPlayerProfile = true,
				GetUserAccountInfo = true,
				GetTitleData = true
			};

			var login = new LoginWithEmailAddressRequest
			{
				Email = email,
				Password = password,
				InfoRequestParameters = infoParams
			};

			var loginData = new LoginData() {IsGuest = false};

			PlayFabClientAPI.LoginWithEmailAddress(login,
				(res) => ProcessAuthentication(res, loginData, onSuccess, onError),
				onError);
		}

		public void ProcessAuthentication(LoginResult result, LoginData loginData, Action<LoginData> onSuccess,
										  Action<PlayFabError> onError)
		{
			var appData = _dataService.GetData<AppData>();
			var titleData = result.InfoResultPayload.TitleData;
			var userId = result.PlayFabId;
			var email = result.InfoResultPayload.AccountInfo.PrivateInfo.Email;
			var userName = result.InfoResultPayload.AccountInfo.Username;
			var emails = result.InfoResultPayload?.PlayerProfile?.ContactEmailAddresses;
			var isMissingContactEmail = emails == null || !emails.Any(e => e != null && e.EmailAddress.Contains("@"));

			_services.HelpdeskService.Login(userId, email, userName);

			if (string.IsNullOrWhiteSpace(appData.DeviceId))
			{
				_services.GameBackendService.LinkDeviceID(null, null);
			}

			if (email != null && email.Contains("@") && isMissingContactEmail)
			{
				_services.GameBackendService.UpdateContactEmail(email);
			}
			
			PlayFabSettings.staticPlayer.CopyFrom(result.AuthenticationContext);

			FLog.Verbose($"Logged in. PlayfabId={result.PlayFabId}");
			//AppleApprovalHack(result);

			if (!titleData.TryGetValue(GameConstants.PlayFab.VERSION_KEY, out var titleVersion))
			{
				throw new Exception($"{GameConstants.PlayFab.VERSION_KEY} not set in title data");
			}

			if (VersionUtils.IsOutdatedVersion(titleVersion))
			{
				OpenGameUpdateDialog(titleVersion);
				return;
			}

			if (titleData.TryGetValue(GameConstants.PlayFab.MAINTENANCE_KEY, out var version) &&
			    VersionUtils.IsOutdatedVersion(version))
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

			_services.AnalyticsService.SessionCalls.PlayerLogin(result.PlayFabId,
				_dataProvider.AppDataProvider.IsGuest);
		}

		public void GetPlayerData()
		{
			_services.GameBackendService.CallFunction("GetPlayerData", OnPlayerDataObtained, OnPlayFabError);
		}

		private void OnPlayerDataObtained(ExecuteFunctionResult res)
		{
			var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(res.FunctionResult.ToString());
			var data = serverResult.Result.Data;

			if (data == null || !data.ContainsKey(typeof(PlayerData).FullName)) // response too large, fetch directly
			{
				_services.GameBackendService.FetchServerState(state =>
				{
					AddDataToService(state);
					FLog.Verbose("Downloaded state from playfab");
				});
				
				return;
			}

			AddDataToService(data);
			FLog.Verbose("Downloaded state from server");
		}
		
		public void AddDataToService(Dictionary<string, string> state)
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
		}

		public void AuthenticatePhoton()
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

		public bool IsAccountDeleted(PlayerData playerData)
		{
			return playerData.Flags.HasFlag(PlayerFlags.Deleted);
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
		
		private void OnPlayFabError(PlayFabError error)
		{
		
		}
	}
}