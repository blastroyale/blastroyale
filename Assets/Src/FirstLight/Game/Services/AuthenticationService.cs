using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Services;
using PlayFab;
using PlayFab.ClientModels;
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
		void ProcessAuthentication(LoginResult result, Action<LoginData> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Calls request to download the player account data
		/// </summary>
		void GetPlayerData();

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
		private IGameDataProvider _dataProvider;

		public PlayfabAuthenticationService(IGameServices services, IDataService dataService, IGameDataProvider dataProvider)
		{
			_services = services;
			_dataService = dataService;
			_dataProvider = dataProvider;
		}

		private void LinkDevice()
		{
			_dataProvider.AppDataProvider.DeviceID.Value = PlayFabSettings.DeviceUniqueIdentifier;
			_dataService.SaveData<AppData>();
		}

		public void LoginSetupGuest(Action<LoginData> onSuccess, Action<PlayFabError> onError)
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
				
				_services.GameBackendService.LinkDeviceID(() =>
				{
					FLog.Verbose("Device linked to new account");
					
					LinkDevice();
					
					onSuccess?.Invoke(new LoginData()
					{
						IsGuest = true
					});
				});
			}, error => onError?.Invoke(error));
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

		public void LoginWithEmail(string email, string password, Action<LoginData> onSuccess, Action<PlayFabError> onError)
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

			PlayFabClientAPI.LoginWithEmailAddress(login, OnLoginSuccess, OnAuthenticationFail);
		}

		public void ProcessAuthentication(LoginResult result, Action<LoginData> onSuccess, Action<PlayFabError> onError)
		{
			var appData = _dataService.GetData<AppData>();
			var userId = result.PlayFabId;
			var email = result.InfoResultPayload.AccountInfo.PrivateInfo.Email;
			var userName = result.InfoResultPayload.AccountInfo.Username;
			var emails = result.InfoResultPayload?.PlayerProfile?.ContactEmailAddresses;
			var isMissingContactEmail = emails == null || !emails.Any(e => e != null && e.EmailAddress.Contains("@"));
			if (email != null && email.Contains("@") && isMissingContactEmail)
			{
				_services.GameBackendService.UpdateContactEmail(email);
			}

			_services.HelpdeskService.Login(userId, email, userName);

			if (string.IsNullOrWhiteSpace(appData.DeviceId))
			{
				_services.GameBackendService.LinkDeviceID(null, null);
			}

			ProcessAuthentication(result);
			
			//// -----
			
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

		public void GetPlayerData()
		{
			throw new NotImplementedException();
		}

		public void AuthenticatePhoton()
		{
			throw new NotImplementedException();
		}

		private bool IsAccountDeleted(PlayerData playerData)
		{
			return playerData.Flags.HasFlag(PlayerFlags.Deleted);
		}
	}
}