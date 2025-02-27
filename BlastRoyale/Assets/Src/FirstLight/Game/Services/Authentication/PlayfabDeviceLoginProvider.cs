using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using FirstLight.Services;
using PlayFab;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services.Authentication
{
	public class PlayfabDeviceLoginProvider : ILoginProvider
	{
		private GetPlayerCombinedInfoRequestParams StandardLoginInfoRequestParams =>
			new ()
			{
				GetPlayerProfile = true,
				GetUserAccountInfo = true,
				GetTitleData = true,
				GetPlayerStatistics = true
			};

		private DataService _localAccountData;

		public PlayfabDeviceLoginProvider()
		{
			_localAccountData = new DataService();
			_localAccountData.LoadData<AccountData>();
			_localAccountData.LoadData<AppData>();
		}

		public UniTask BeforeAuthentication(bool previouslyLoggedIn)
		{
			return UniTask.CompletedTask;
		}

		public async UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn)
		{
			await LinkAccountToDevice(result);
		}

		public UniTask AfterFetchedState(LoginResult result)
		{
			return UniTask.CompletedTask;
			;
		}

		public string GetStoredDeviceID()
		{
			var accountData = _localAccountData.GetData<AccountData>();
			var deviceId = accountData.DeviceId;
			if (!string.IsNullOrEmpty(deviceId)) return deviceId;
			return null;
		}

		/// <summary>
		/// Migrating where reads login data from old players
		/// </summary>
		private void AccountReadTrick()
		{
			var appData = _localAccountData.GetData<AppData>();

#pragma warning disable CS0612 // Here for backwards compatability
			if (!string.IsNullOrWhiteSpace(appData.DeviceId))
			{
				var accountData = GetAccountData();
				accountData.DeviceId = appData.DeviceId;
				accountData.LastLoginEmail = appData.LastLoginEmail;
				appData.DeviceId = null;
				appData.LastLoginEmail = null;
				_localAccountData.AddData(appData, true);
				_localAccountData.SaveData<AppData>();
				SaveAccountData();
			}
#pragma warning restore CS0612
		}

		public async UniTask<LoginResult> LoginWithDevice()
		{
			AccountReadTrick();
			FLog.Info("Found device id " + _localAccountData.GetData<AccountData>().DeviceId);
			if (HasLinkedAccount())
			{
				return await LoginIntoLinkedAccount();
			}

			return await CreateGuestAccountAndLogin();
		}

		public async UniTask SendAccountRecoveryEmail(string email)
		{
			var request = new SendAccountRecoveryEmailRequest
			{
				TitleId = PlayFabSettings.TitleId,
				Email = email,
				EmailTemplateId = FLEnvironment.Current.PlayFabRecoveryEmailTemplateID,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			await AsyncPlayfabAPI.ClientAPI.SendAccountRecoveryEmail(request);
		}

		public async UniTask AddUserNamePassword(string email, string username, string password)
		{
			var addUsernamePasswordRequest = new AddUsernamePasswordRequest
			{
				Email = email,
				Username = username,
				Password = password
			};

			await AsyncPlayfabAPI.ClientAPI.AddUsernamePassword(addUsernamePasswordRequest);
		}

		public async UniTask Logout()
		{
			var data = GetAccountData();
			data.PlayfabID = "";
			data.DeviceId = "";
			SaveAccountData();
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			var unlinkRequest = new UnlinkCustomIDRequest
			{
				CustomId = ParrelHelpers.DeviceID()
			};

			await AsyncPlayfabAPI.ClientAPI.UnlinkCustomID(unlinkRequest);
#elif UNITY_ANDROID
			var unlinkRequest = new UnlinkAndroidDeviceIDRequest
			{
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};
			await AsyncPlayfabAPI.ClientAPI.UnlinkAndroidDeviceID(unlinkRequest);
#elif UNITY_IOS
			var unlinkRequest = new UnlinkIOSDeviceIDRequest
			{
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};
			await AsyncPlayfabAPI.ClientAPI.UnlinkIOSDeviceID(unlinkRequest);

#endif
		}

		public async UniTask LinkAccountToDevice(LoginResult loginResult)
		{
			// Already linked
			if (HasLinkedAccount() && loginResult.PlayFabId == GetAccountData().PlayfabID)
			{
				return;
			}

			await SendLinkAccountPlayfabRequest();
			var data = GetAccountData();
			data.PlayfabID = loginResult.PlayFabId;
			data.DeviceId = ParrelHelpers.DeviceID();
			SaveAccountData();
		}

		public async UniTask<LoginResult> LoginIntoLinkedAccount()
		{
			var deviceId = GetStoredDeviceID();

#if UNITY_EDITOR
			{
				var login = new LoginWithCustomIDRequest
				{
					CreateAccount = false,
					CustomId = deviceId,
					InfoRequestParameters = StandardLoginInfoRequestParams
				};
				return await AsyncPlayfabAPI.ClientAPI.LoginWithCustomID(login);
			}
#elif UNITY_ANDROID
			{
				var login = new LoginWithAndroidDeviceIDRequest()
				{
					CreateAccount = false,
					AndroidDevice = UnityEngine.SystemInfo.deviceModel,
					OS = UnityEngine.SystemInfo.operatingSystem,
					AndroidDeviceId = deviceId,
					InfoRequestParameters = StandardLoginInfoRequestParams
				};
				return await AsyncPlayfabAPI.ClientAPI.LoginWithAndroidDeviceID(login);
			}

#elif UNITY_IOS
			{
				var login = new LoginWithIOSDeviceIDRequest()
				{
					CreateAccount = false,
					DeviceModel = UnityEngine.SystemInfo.deviceModel,
					OS = UnityEngine.SystemInfo.operatingSystem,
					DeviceId = deviceId,
					InfoRequestParameters = StandardLoginInfoRequestParams
				};
				return await AsyncPlayfabAPI.ClientAPI.LoginWithIOSDeviceID(login);
			}
#endif
		}

		public async UniTask<LoginResult> CreateGuestAccountAndLogin()
		{
			var login = new LoginWithCustomIDRequest
			{
				CreateAccount = true,
				CustomId = Guid.NewGuid().ToString(),
				InfoRequestParameters = StandardLoginInfoRequestParams
			};

			return await AsyncPlayfabAPI.ClientAPI.LoginWithCustomID(login);
		}

		public async UniTask SendLinkAccountPlayfabRequest()
		{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			{
				var link = new LinkCustomIDRequest
				{
					CustomId = ParrelHelpers.DeviceID(),
					ForceLink = true
				};

				await AsyncPlayfabAPI.ClientAPI.LinkCustomID(link);
			}
#elif UNITY_ANDROID
			{
				var link = new LinkAndroidDeviceIDRequest
				{
					AndroidDevice = UnityEngine.SystemInfo.deviceModel,
					OS = UnityEngine.SystemInfo.operatingSystem,
					AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
					ForceLink = true
				};

				await AsyncPlayfabAPI.ClientAPI.LinkAndroidDeviceID(link);
			}

#elif UNITY_IOS
			{
				var link = new LinkIOSDeviceIDRequest
				{
					DeviceModel = UnityEngine.SystemInfo.deviceModel,
					OS = UnityEngine.SystemInfo.operatingSystem,
					DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
					ForceLink = true
				};

				await AsyncPlayfabAPI.ClientAPI.LinkIOSDeviceID(link);
			}
#endif
		}

		private bool HasLinkedAccount()
		{
			return !string.IsNullOrEmpty(GetAccountData().DeviceId);
		}

		public async UniTask<LoginResult> LoginWithEmailPassword(string email, string password)
		{
			var login = new LoginWithEmailAddressRequest
			{
				Email = email,
				Password = password,
				InfoRequestParameters = StandardLoginInfoRequestParams
			};
			return await AsyncPlayfabAPI.ClientAPI.LoginWithEmailAddress(login);
		}

		private AccountData GetAccountData()
		{
			return _localAccountData.GetData<AccountData>();
		}

		private void SaveAccountData()
		{
			_localAccountData.SaveData<AccountData>();
		}
	}
}