using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLightServerSDK.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.SharedModels;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FirstLight.Game.Services
{
	public class LoginData
	{
		public bool IsGuest;
		public string Email;
		public string Username;
		public string DisplayName;
		public string Password;
	}

	public class AuthenticationState
	{
		public bool LastAttemptFailed;
		public int Retries;
		public bool StartedWithAccount;
		public bool LoggedIn;
	}

	/// <summary>
	/// This services handles all authentication functionality
	/// </summary>
	public interface IAuthenticationService
	{
		/// <summary>
		/// State of the current authentication flow
		/// </summary>
		AuthenticationState State { get; }
		
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
		void LoginWithEmail(string email, string password, Action<LoginData> onSuccess, Action<PlayFabError> onError, bool previouslyLoggedIn = false);

		/// <summary>
		/// Logs out of the current account. This includes unlinking the device, and logging out of other services
		/// </summary>
		void Logout(Action onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Registers the user on the backend with the provided credentials.
		/// Validation of credentials should be done at UI level so user is less likely to encounter errors
		/// </summary>
		void RegisterWithEmail(string email, string username, string displayName, string password,
							   Action<LoginData> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Updates anonymous account with provided registration data
		/// </summary>
		void AttachLoginDataToAccount(string email, string username, string password,
									  Action<LoginData> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Sends account recovery email to the specified address. 
		/// </summary>
		void SendAccountRecoveryEmail(string email, Action onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Requests to check if the current authenticated account is flagged for deletion
		/// </summary>
		/// <returns></returns>
		bool IsAccountDeleted();

		/// <summary>
		/// Checks if the logged in player is a guest
		/// returns false if not logged in
		/// </summary>
		bool IsGuest { get; }
		
		/// <summary>
		/// Returns true if the player has stored a linked device in his local AccountData
		/// </summary>
		bool IsDeviceLinked { get; }
		
		/// <summary>
		/// Sets linked device to the current device context.
		/// This is automatically set when logging in, but in case of failed logins, can be called to immediately to
		/// update the status of the device link locally.
		/// </summary>
		void SetLinkedDevice(bool isLinked);

		/// <summary>
		/// Gets the current account data stored on device
		/// </summary>
		AccountData GetDeviceSavedAccountData();
		
		/// <summary>
		/// Event called after players logs in
		/// </summary>
		event Action<LoginResult> OnLogin;
	}

	public interface IInternalAuthenticationService : IAuthenticationService
	{
		/// <summary>
		/// Processes authentication response.
		/// This includes setting auth context, parsing feature flags, parsing remote configs, saving relevant local data, etc.
		/// </summary>
		void ProcessAuthentication(LoginResult result, LoginData loginData, Action<LoginData> onSuccess,
								   Action<PlayFabError> onError, bool previouslyLoggedIn);

		/// <summary>
		/// Calls request to download the player account data
		/// </summary>
		void GetPlayerData(LoginData loginData, Action<LoginData> onSuccess, Action<PlayFabError> onError, bool previouslyLoggedIn);

		/// <summary>
		/// Deserializes and adds the obtained player state data into data service
		/// </summary>
		void UpdatePlayerDataAndLogic(Dictionary<string, string> data, bool previouslyLoggedIn);

		/// <summary>
		/// Authenticates the game network with the processed authentication data
		/// </summary>
		void AuthenticateGameNetwork(LoginData loginData, Action<LoginData> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Updates user contact email address
		/// </summary>
		void UpdateContactEmail(string newEmail, Action<AddOrUpdateContactEmailResult> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Links this device current account
		/// </summary>
		void LinkDeviceID(Action onSuccess = null, Action<PlayFabError> onError = null);

		/// <summary>
		/// Unlinks this device current account
		/// </summary>
		void UnlinkDeviceID(Action onSuccess = null, Action<PlayFabError> errorCallback = null);

		/// <summary>
		/// Attempts to migrate data between accounts if the correct conditions have been met
		/// </summary>
		void TryMigrateData(MigrationData migrationData);
	}

	/// <inheritdoc cref="IAuthenticationService" />
	public class PlayfabAuthenticationService : IInternalAuthenticationService
	{
		public event Action<LoginResult> OnLogin;
		
		private IGameLogicInitializer _logicInit;
		private IGameServices _services;
		private IDataService _dataService;
		private IInternalGameNetworkService _networkService;
		private IGameDataProvider _dataProvider;
		private IConfigsAdder _configsAdder;
		private readonly DataService _localAccountData;

		private GetPlayerCombinedInfoRequestParams StandardLoginInfoRequestParams =>
			new()
			{
				GetPlayerProfile = true,
				GetUserAccountInfo = true,
				GetTitleData = true,
				GetPlayerStatistics = true
			};

		public PlayfabAuthenticationService(IGameLogicInitializer logicInit, IGameServices services, IDataService dataService,
											IInternalGameNetworkService networkService,
											IGameDataProvider dataProvider, IConfigsAdder configsAdder)
		{
			_logicInit = logicInit;
			_services = services;
			_dataService = dataService;
			_networkService = networkService;
			_dataProvider = dataProvider;
			_configsAdder = configsAdder;
			_localAccountData = new DataService();
			_localAccountData.LoadData<AccountData>();
			State = new AuthenticationState()
			{
				Retries = 0,
				LastAttemptFailed = false,
				StartedWithAccount = false,
			};
		}

		public AccountData GetDeviceSavedAccountData()
		{
			return _localAccountData.GetData<AccountData>();
		}

		public bool IsDeviceLinked => !string.IsNullOrWhiteSpace(GetDeviceSavedAccountData().DeviceId);

		public bool IsGuest => string.IsNullOrEmpty(GetDeviceSavedAccountData().LastLoginEmail);
		
		public AuthenticationState State { get; }

		public void LoginSetupGuest(Action<LoginData> onSuccess, Action<PlayFabError> onError)
		{
			FLog.Verbose($"Creating guest account");

			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = true,
				CustomId = Guid.NewGuid().ToString(),
				InfoRequestParameters = StandardLoginInfoRequestParams
			};

			var loginData = new LoginData() {IsGuest = true};

			PlayFabClientAPI.LoginWithCustomID(login,
				(res) => ProcessAuthentication(res, loginData, onSuccess, onError), e => { _services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Login); });
		}

		public void LoginWithDevice(Action<LoginData> onSuccess, Action<PlayFabError> onError)
		{
			FLog.Verbose("Logging in with device ID");

			var deviceId = _localAccountData.GetData<AccountData>().DeviceId;
			var loginData = new LoginData() {IsGuest = false};

#if UNITY_EDITOR
			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = false,
				CustomId = deviceId,
				InfoRequestParameters = StandardLoginInfoRequestParams
			};

			PlayFabClientAPI.LoginWithCustomID(login,
				(res) => ProcessAuthentication(res, loginData, onSuccess, onError), e => { _services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Login); });
#elif UNITY_ANDROID
			var login = new LoginWithAndroidDeviceIDRequest()
			{
				CreateAccount = false,
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = deviceId,
				InfoRequestParameters = StandardLoginInfoRequestParams
			};
			
			PlayFabClientAPI.LoginWithAndroidDeviceID(login, (res) => ProcessAuthentication(res, loginData, onSuccess, onError),e =>
				{
					_services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Login);
				});
#elif UNITY_IOS
			var login = new LoginWithIOSDeviceIDRequest()
			{
				CreateAccount = false,
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = deviceId,
				InfoRequestParameters = StandardLoginInfoRequestParams
			};

			PlayFabClientAPI.LoginWithIOSDeviceID(login, (res) => ProcessAuthentication(res, loginData, onSuccess, onError),e =>
				{
					_services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Login);
				});
#endif
		}

		public void LoginWithEmail(string email, string password, Action<LoginData> onSuccess,
								   Action<PlayFabError> onError, bool previouslyLoggedIn = false)
		{
			var login = new LoginWithEmailAddressRequest
			{
				Email = email,
				Password = password,
				InfoRequestParameters = StandardLoginInfoRequestParams
			};

			var loginData = new LoginData() {IsGuest = false};
			PlayFabClientAPI.LoginWithEmailAddress(login,
				(res) => ProcessAuthentication(res, loginData, onSuccess, onError, previouslyLoggedIn), e => { _services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Login); });
		}

		public void Logout(Action onSuccess, Action<PlayFabError> onError)
		{
			UnlinkDeviceID(() =>
			{
				_services.HelpdeskService.Logout();
				var data = _localAccountData.GetData<AccountData>();
				data.LastLoginEmail = null;
				data.DeviceId = null;
				_localAccountData.SaveData<AccountData>();
				onSuccess?.Invoke();
			}, e => { _services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Login); });
		}

		public void RegisterWithEmail(string email, string username, string displayName, string password,
									  Action<LoginData> onSuccess,
									  Action<PlayFabError> onError)
		{
			var register = new RegisterPlayFabUserRequest
			{
				Email = email,
				DisplayName = username,
				Username = username,
				Password = password
			};

			var loginData = new LoginData
			{
				Email = email,
				Username = username,
				DisplayName = displayName,
				Password = password
			};

			PlayFabClientAPI.RegisterPlayFabUser(register, _ => onSuccess(loginData), e => { _services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Login); });
		}


		public void ProcessAuthentication(LoginResult result, LoginData loginData, Action<LoginData> onSuccess,
										  Action<PlayFabError> onError, bool previouslyLoggedIn = false)
		{
			if (FeatureFlags.GetLocalConfiguration().ForceAuthError)
			{
				onError(new PlayFabError()
				{
					Error = PlayFabErrorCode.ConnectionError,
					ErrorMessage = "Simulated connection error"
				});
				return;
			}

			FLog.Info($"Logged in. PlayfabId={result.PlayFabId} Title={PlayFabSettings.TitleId}");

			var accountData = GetDeviceSavedAccountData();
			var appData = _dataService.GetData<AppData>();
			var tutorialData = _dataService.GetData<TutorialData>();
			var titleData = result.InfoResultPayload.TitleData;

			if (titleData.TryGetValue("REDIRECT_TESTSERVER", out var version))
			{
				if (version == VersionUtils.VersionExternal)
				{
					FLog.Info("Redirecting to staging");
					_services.GameBackendService.SetupBackendEnvironment(Environment.STAGING);
					LoginSetupGuest(onSuccess, onError);
					return;
				}
			}
			else
			{
				FLog.Info($"No server redirect version={VersionUtils.VersionExternal} vInternal {VersionUtils.VersionInternal}");
			}
			
			var userId = result.PlayFabId;
			var email = result.InfoResultPayload.AccountInfo.PrivateInfo.Email;
			var userName = result.InfoResultPayload.AccountInfo.Username;
			var emails = result.InfoResultPayload.PlayerProfile?.ContactEmailAddresses;
			var isMissingContactEmail = emails == null || !emails.Any(e => e != null && e.EmailAddress.Contains("@"));
			var migrationData = new MigrationData() {TutorialSections = tutorialData.TutorialSections};
			_networkService.UserId.Value = result.PlayFabId;

			//AppleApprovalHack(result);
			if (titleData.TryGetValue("PHOTON_APP", out var photonAppId))
			{
				var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;
				quantumSettings.AppSettings.AppIdRealtime = photonAppId;
				_services.GameBackendService.CurrentEnvironmentData.AppIDRealtime = photonAppId;
				FLog.Info("Setting up photon app id by playfab title data " + photonAppId);
			}

			FLog.Info("Using photon with the id " + _services.GameBackendService.CurrentEnvironmentData.AppIDRealtime);

			var requiredServices = 2;
			var doneServices = 0;

			void OnServiceConnected(LoginData data)
			{
				if (++doneServices >= requiredServices)
				{
					if (previouslyLoggedIn)
					{
						TryMigrateData(migrationData);
					}

					_dataService.SaveData<AppData>();
					onSuccess(loginData);
				}
			}

			AuthenticateGameNetwork(loginData, OnServiceConnected, onError);
			GetPlayerData(loginData, OnServiceConnected, onError, previouslyLoggedIn);

			if (!titleData.TryGetValue(GameConstants.PlayFab.VERSION_KEY, out var titleVersion))
			{
				onError?.Invoke(null);
				throw new Exception($"{GameConstants.PlayFab.VERSION_KEY} not set in title data");
			}

			_services.HelpdeskService.Login(userId, email, userName);


			if (string.IsNullOrWhiteSpace(accountData.DeviceId) || result.InfoResultPayload.AccountInfo.PrivateInfo.Email != accountData.LastLoginEmail)
			{
				LinkDeviceID(null, null);
			}

			if (email != null && email.Contains("@") && isMissingContactEmail)
			{
				UpdateContactEmail(email, null, null);
			}

			FeatureFlags.ParseFlags(titleData);
			FeatureFlags.ParseLocalFeatureFlags();
			_services.MessageBrokerService.Publish(new FeatureFlagsChanged());


			_services.LiveopsService.FetchSegments(_ =>
			{
				var liveopsFeatureFlags = _services.LiveopsService.GetUserSegmentedFeatureFlags();
				FeatureFlags.ParseFlags(liveopsFeatureFlags);
				_services.MessageBrokerService.Publish(new FeatureFlagsChanged());
			});

			_networkService.UserId.Value = result.PlayFabId;
			appData.DisplayName = result.InfoResultPayload.AccountInfo.TitleInfo.DisplayName;
			appData.FirstLoginTime = result.InfoResultPayload.AccountInfo.Created;
			appData.AvatarUrl = result.InfoResultPayload.AccountInfo.TitleInfo.AvatarUrl;
			appData.LoginTime = _services.TimeService.DateTimeUtcNow;
			appData.LastLoginTime = result.LastLoginTime ?? result.InfoResultPayload.AccountInfo.Created;
			appData.IsFirstSession = result.NewlyCreated;
			appData.PlayerId = result.PlayFabId;
			accountData.LastLoginEmail = result.InfoResultPayload.AccountInfo.PrivateInfo.Email;
			appData.TitleData = titleData;
			OnLogin?.Invoke(result);
			State.LoggedIn = true;
			_dataService.SaveData<AppData>();
			_localAccountData.SaveData<AccountData>();
			FLog.Verbose("Saved AppData");

			_services.AnalyticsService.SessionCalls.PlayerLogin(result.PlayFabId, IsGuest);
		}

		public void GetPlayerData(LoginData loginData, Action<LoginData> onSuccess, Action<PlayFabError> onError, bool previouslyLoggedIn)
		{
			_services.GameBackendService.CallFunction("GetPlayerData",
				(res) => { OnPlayerDataObtained(res, loginData, onSuccess, onError, previouslyLoggedIn); }, e => { _services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Login); });
		}

		private void OnPlayerDataObtained(ExecuteFunctionResult res, LoginData loginData, Action<LoginData> onSuccess,
										  Action<PlayFabError> onError, bool previouslyLoggedIn)
		{
			var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(res.FunctionResult.ToString());
			var data = serverResult.Result.Data;

			if (data != null)
			{
				if (data.TryGetValue("BuildNumber", out var buildNumber))
				{
					VersionUtils.ServerBuildNumber = buildNumber;
				}

				if (data.TryGetValue("BuildCommit", out var buildCommit))
				{
					VersionUtils.ServerBuildCommit = buildCommit;
				}

				VersionUtils.ValidateServer();
			}

			if (data == null || !data.ContainsKey(typeof(PlayerData).FullName)) // response too large, fetch directly
			{
				_services.GameBackendService.FetchServerState(state =>
				{
					FLog.Verbose("Downloaded state from playfab");
					UpdatePlayerDataAndLogic(state, previouslyLoggedIn);
					AuthenticateGameNetwork(loginData, onSuccess, onError);
				}, onError);

				return;
			}

			FLog.Verbose("Downloaded state from server");
			UpdatePlayerDataAndLogic(data, previouslyLoggedIn);
			AuthenticateGameNetwork(loginData, onSuccess, onError);
		}

		public void UpdatePlayerDataAndLogic(Dictionary<string, string> state, bool previouslyLoggedIn)
		{
			foreach (var typeFullName in state.Keys)
			{
				try
				{
					var type = Assembly.GetExecutingAssembly().GetType(typeFullName);
					// Type does not exists any more, example SeasonData
					if (type == null)
					{
						continue;
					}
					var dataInstance = ModelSerializer.DeserializeFromData(type, state);
					if (dataInstance is CollectionItemEnrichmentData enrichmentData)
					{
						_services.CollectionEnrichnmentService.Enrich(enrichmentData);
					}

					_dataService.AddData(type, dataInstance);
				}
				catch (Exception e)
				{
					FLog.Error("Error reading data type " + typeFullName, e);
				}
			}

			if (previouslyLoggedIn)
			{
				_logicInit.ReInit();
				_services.MessageBrokerService.Publish(new ReinitializeMenuViewsMessage());
			}
		}

		public void AuthenticateGameNetwork(LoginData loginData, Action<LoginData> onSuccess,
											Action<PlayFabError> onError)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var appId = config.PhotonServerSettings.AppSettings.AppIdRealtime;
			var request = new GetPhotonAuthenticationTokenRequest {PhotonApplicationId = appId};

			PlayFabClientAPI.GetPhotonAuthenticationToken(request, OnAuthSuccess, onError);

			void OnAuthSuccess(GetPhotonAuthenticationTokenResult result)
			{
				_networkService.QuantumClient.AuthValues.AddAuthParameter("token", result.PhotonCustomAuthenticationToken);
				_services.NetworkService.ConnectPhotonServer();
				onSuccess?.Invoke(loginData);
			}
		}

		public void UpdateContactEmail(string newEmail, Action<AddOrUpdateContactEmailResult> onSuccess, Action<PlayFabError> onError)
		{
			FLog.Info("Updating user email to " + newEmail);
			var emailUpdate = new AddOrUpdateContactEmailRequest()
			{
				EmailAddress = newEmail
			};

			PlayFabClientAPI.AddOrUpdateContactEmail(emailUpdate, onSuccess, e => { _services.GameBackendService.HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session); });
		}

		public void LinkDeviceID(Action onSuccess = null, Action<PlayFabError> onError = null)
		{
#if UNITY_EDITOR
			var link = new LinkCustomIDRequest
			{
				CustomId = ParrelHelpers.DeviceID(),
				ForceLink = true
			};

			PlayFabClientAPI.LinkCustomID(link, _ => OnSuccess(), onError);
#elif UNITY_ANDROID
			var link = new LinkAndroidDeviceIDRequest
			{
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkAndroidDeviceID(link, _ => OnSuccess(), onError);

#elif UNITY_IOS
			var link = new LinkIOSDeviceIDRequest
			{
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkIOSDeviceID(link, _ => OnSuccess(), onError);
#endif
			void OnSuccess()
			{
				_services.AuthenticationService.SetLinkedDevice(true);
				onSuccess?.Invoke();
			}
		}

		public void UnlinkDeviceID(Action onSuccess = null, Action<PlayFabError> errorCallback = null)
		{
#if UNITY_EDITOR
			var unlinkRequest = new UnlinkCustomIDRequest
			{
				CustomId = ParrelHelpers.DeviceID()
			};

			PlayFabClientAPI.UnlinkCustomID(unlinkRequest, _ => OnSuccess(), errorCallback);
#elif UNITY_ANDROID
			var unlinkRequest = new UnlinkAndroidDeviceIDRequest
			{
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};
			
			PlayFabClientAPI.UnlinkAndroidDeviceID(unlinkRequest, _ => OnSuccess(), errorCallback);
#elif UNITY_IOS
			var unlinkRequest = new UnlinkIOSDeviceIDRequest
			{
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};

			PlayFabClientAPI.UnlinkIOSDeviceID(unlinkRequest, _ => OnSuccess(), errorCallback);
#endif
			void OnSuccess()
			{
				_services.AuthenticationService.SetLinkedDevice(false);
				onSuccess?.Invoke();
			}
		}

		public void TryMigrateData(MigrationData migrationData)
		{
			_services.CommandService.ExecuteCommand(new MigrateGuestDataCommand {GuestMigrationData = migrationData});
		}

		public void AttachLoginDataToAccount(string email, string username, string password,
											 Action<LoginData> onSuccess = null,
											 Action<PlayFabError> onError = null)
		{
			var addUsernamePasswordRequest = new AddUsernamePasswordRequest
			{
				Email = email,
				Username = username,
				Password = password
			};

			var loginData = new LoginData()
			{
				Email = email,
				Username = username,
				Password = password
			};

			PlayFabClientAPI.AddUsernamePassword(addUsernamePasswordRequest, OnSuccess, onError);

			void OnSuccess(AddUsernamePasswordResult result)
			{
				_localAccountData.GetData<AccountData>().LastLoginEmail = email;
				_localAccountData.SaveData<AccountData>();
				_services.GameBackendService.UpdateDisplayName(result.Username, null, null);
				onSuccess?.Invoke(loginData);
			}
		}

		public void SendAccountRecoveryEmail(string email, Action onSuccess, Action<PlayFabError> onError)
		{
			SendAccountRecoveryEmailRequest request = new SendAccountRecoveryEmailRequest()
			{
				TitleId = PlayFabSettings.TitleId,
				Email = email,
				EmailTemplateId = _services.GameBackendService.CurrentEnvironmentData.RecoveryEmailTemplateID,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			PlayFabClientAPI.SendAccountRecoveryEmail(request, _ => { onSuccess?.Invoke(); }, onError);
		}

		public void SetLinkedDevice(bool linked)
		{
			_localAccountData.GetData<AccountData>().DeviceId = linked ? ParrelHelpers.DeviceID() : "";
			_localAccountData.SaveData<AccountData>();
		}

		public bool IsAccountDeleted()
		{
			return _dataService.GetData<PlayerData>().Flags.HasFlag(PlayerFlags.Deleted);
		}
	}
}