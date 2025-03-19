using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.Authentication.Hooks;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Services;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace FirstLight.Game.Services.Authentication
{
	public interface INativeGamesLoginProvider
	{
		public UniTask<bool> CanAuthenticate();
		public UniTask<LoginResult> Authenticate();

		public UniTask TryToLinkAccount(LoginResult result);
	}

	public class NullNativeGamesLogin : INativeGamesLoginProvider
	{
		public UniTask<bool> CanAuthenticate()
		{
			return UniTask.FromResult(false);
		}

		public UniTask<LoginResult> Authenticate()
		{
			return UniTask.FromResult<LoginResult>(null);
		}

		public UniTask TryToLinkAccount(LoginResult result)
		{
			return UniTask.CompletedTask;
		}
	}

	public interface IAuthenticationHook
	{
		UniTask BeforeAuthentication(bool previouslyLoggedIn);
		UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn);
		UniTask AfterFetchedState(LoginResult result);
		UniTask BeforeLogout();
	}

	internal interface ILoginProvider : IAuthenticationHook
	{
		public UniTask Logout();
		public UniTask<LoginResult> LoginWithEmailPassword(string email, string password);
		public UniTask<LoginResult> LoginWithDevice();
		public UniTask<LoginResult> LoginWithNewGuestAccount();
		public UniTask AddUserNamePassword(string email, string username, string password);
		public UniTask SendAccountRecoveryEmail(string email);
		public bool CanLoginAutomatically();
	}

	public interface ISessionData
	{
		public bool IsAuthenticated { get; }
		public bool IsFirstSession { get; }
		public bool IsGuest { get; }
		public string Email { get; }
	}

	public interface IAuthService
	{
		/**
		* The local player display name, it has not parsed or escaped spaces and etc
		* If you want pretty display names check the <see cref="AuthServiceNameExtensions"/> class
		*/
		string RawLocalPlayerName { get; }

		/// <summary>
		/// It will go through the whole login process with a NEW guest account and link to the device at the end
		/// </summary>
		/// <returns></returns>
		UniTask LoginWithGuestAccount();

		/// <summary>
		/// It will go through the whole login process with the stored/linked device account
		/// </summary>
		/// <returns></returns>
		UniTask LoginWithDeviceID();

		/// <summary>
		/// It will go through the whole login process with the email and password provided, it may fail if the input is incorrect
		/// </summary>
		/// <returns></returns>
		UniTask LoginWithEmailProcess(string email, string password);

		/// <summary>
		/// It will go through the whole login process with the Native games service, Google Play Games or Apple Game Center depending on the platform
		/// </summary>
		/// <returns></returns>
		public UniTask LoginWithNativeGamesService();

		/// <summary>
		/// It will go through the whole login process with a previous processed login result,
		/// this can be useful if you to check if a given account is valid before going through the whole authentication flow
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		public UniTask LoginWithExistingResult(LoginResult result);

		/// <summary>
		/// It will try to link the current logged in game account services to the account used in game
		/// this is used when user already has an account and wants to link a game center account to it
		/// </summary>
		/// <returns></returns>
		public UniTask TryToLinkNativeAccount();

		/// <summary>
		/// Check if the native game services are enabled and available on the device
		/// </summary>
		/// <returns></returns>
		public UniTask<bool> CanLoginWithNativeGamesService();

		/// <summary>
		/// Fetch the login result for the email and password, this does NOT go through the authentication flow and do not change the auth state
		/// If you want to login with those credentials you must pass the result to <see cref="LoginWithExistingResult"/>
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public UniTask<LoginResult> FetchCredentialsWithEmailPassword(string email, string password);

		/// <summary>
		/// Set the player display name and return a error message if any happens
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		UniTask<string> SetDisplayName(string name);

		/// <summary>
		/// Register an email and password for the current authenticated account
		/// </summary>
		/// <returns></returns>
		public UniTask AddUserNamePassword(string email, string username, string password);

		/// <summary>
		/// Data for the authenticated account and session
		/// </summary>
		public ISessionData SessionData { get; }

		/// <summary>
		/// Send an recovery account email to the account, this is handled by playfab
		/// </summary>
		/// <param name="email"></param>
		/// <returns></returns>
		UniTask SendAccountRecoveryEmail(string email);

		/// <summary>
		/// Logout the user and remove it from the authentication data
		/// </summary>
		/// <returns></returns>
		UniTask Logout();

		/// <summary>
		/// Delete the current account and logout
		/// </summary>
		/// <returns></returns>
		UniTask DeleteAccount();

		/// <summary>
		/// Check if the user has a linked account to the device, this is used for the second time an user opens the game
		/// </summary>
		/// <returns></returns>
		public bool HasDeviceLinked();
		
		/// <summary>
		/// Registers an authentication hook
		/// </summary>
		public void RegisterHook(IAuthenticationHook hook);
		
		/// <summary>
		/// Currently linked login types to this account
		/// </summary>
		public IReadOnlyCollection<LoginType> LoginTypes { get; }

		/// <summary>
		/// Returns if user is able to link current account to a native account
		/// </summary>
		public UniTask<bool> CanLinkToNativeAccount();
	}

	public enum LoginType
	{
		Native, LegacyDevice, EmailPass
	}

	public class SessionData : ISessionData
	{
		public bool IsAuthenticated { get; set; }
		public bool IsFirstSession { get; set; }
		public bool IsGuest { get; set; }
		public string Email { get; set; }

		internal LoginResult LastLoginResult;
	}

	public class AuthService : IAuthService
	{
		private IGameBackendService _backendService;

		private ILoginProvider _loginProvider;

		private INativeGamesLoginProvider _nativeGamesLogin;

		private readonly IMessageBrokerService _msgBroker;
		private readonly IGameLogicInitializer _logicInitializer;
		private readonly IDataService _dataService;

		public IReadOnlyCollection<LoginType> LoginTypes { get; private set; } 
		public async UniTask<bool> CanLinkToNativeAccount()
		{
			return LoginTypes.Contains(LoginType.EmailPass) && !LoginTypes.Contains(LoginType.Native) && await CanLoginWithNativeGamesService();
		}

		public string RawLocalPlayerName { get; private set; }
		public ISessionData SessionData => _sessionData;
		private SessionData _sessionData;

		private List<IAuthenticationHook> _authenticationHooks;

		public AuthService(
			IGameLogicInitializer logicInitializer,
			IGameBackendService backendService,
			IDataService dataService,
			IGameRemoteConfigProvider gameRemoteConfigProvider,
			IGameCommandService gameCommandService,
			IMessageBrokerService msgBroker, IInternalGameNetworkService networkService)
		{
			_sessionData = new SessionData();
			_logicInitializer = logicInitializer;
			_dataService = dataService;
			_msgBroker = msgBroker;
			_backendService = backendService;
			_msgBroker = msgBroker;
			_loginProvider = new PlayfabDeviceLoginProvider();
			var unityCloudHook = new UnityCloudHook(backendService, dataService, gameRemoteConfigProvider, msgBroker);
			unityCloudHook.OnSetPlayfabName += OnUnityAuthSetPlayfabName;
			_authenticationHooks = new IAuthenticationHook[]
			{
				_loginProvider,
				unityCloudHook,
				new MigrateTutorialDataHook(dataService, gameCommandService),
				new ParseFeatureFlagsHook(msgBroker),
				new AuthenticatePhotonHook(networkService),
				new UpdateContactEmailHook()
			}.ToList();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
			_nativeGamesLogin = new NativeAuthenticationProvider();
#else
			_nativeGamesLogin = new NullNativeGamesLogin();
#endif
		}

		/// <summary>
		/// Called when copying name from unity to playfab, used for name generation
		/// </summary>
		private void OnUnityAuthSetPlayfabName(string name)
		{
			RawLocalPlayerName = name;
			_msgBroker.Publish(new DisplayNameChangedMessage()
			{
				NewPlayfabDisplayName = name
			});
		}

		public UniTask LoginWithNativeGamesService()
		{
			_sessionData.Email = null;
			_sessionData.IsAuthenticated = false;
			_sessionData.IsFirstSession = true;
			return LoginProcess(_nativeGamesLogin.Authenticate(), false);
		}

		public async UniTask TryToLinkNativeAccount()
		{
			try
			{
				await _nativeGamesLogin.TryToLinkAccount(_sessionData.LastLoginResult);
			}
			catch (Exception ex)
			{
				FLog.Warn("Failed to link native account", ex);
			}
		}

		public UniTask<bool> CanLoginWithNativeGamesService()
		{
			return _nativeGamesLogin.CanAuthenticate();
		}

		public UniTask LoginWithGuestAccount()
		{
			_sessionData.Email = null;
			_sessionData.IsAuthenticated = false;
			_sessionData.IsFirstSession = true;
			return LoginProcess(_loginProvider.LoginWithNewGuestAccount(), false);
		}

		public UniTask LoginWithDeviceID()
		{
			_sessionData.Email = null;
			_sessionData.IsAuthenticated = false;
			_sessionData.IsFirstSession = false;
			return LoginProcess(_loginProvider.LoginWithDevice(), false);
		}

		public async UniTask LoginWithEmailProcess(string email, string password)
		{
			await LoginProcess(_loginProvider.LoginWithEmailPassword(email, password), true);
		}

		public UniTask<LoginResult> FetchCredentialsWithEmailPassword(string email, string password)
		{
			return _loginProvider.LoginWithEmailPassword(email, password);
		}

		public UniTask LoginWithExistingResult(LoginResult result)
		{
			return LoginProcess(UniTask.FromResult(result));
		}

		private async UniTask LoginProcess(UniTask<LoginResult> task, bool previouslyLoggedIn = false)
		{
			await UniTask.WhenAll(_authenticationHooks.Select(a => a.BeforeAuthentication(previouslyLoggedIn)));

			var loginResult = await task;
			_sessionData.LastLoginResult = loginResult;
			_sessionData.IsFirstSession = loginResult.NewlyCreated;
			var hasEmail = string.IsNullOrWhiteSpace(loginResult.InfoResultPayload.AccountInfo.PrivateInfo.Email);
			
			_sessionData.IsGuest = !hasEmail;
			var logins = new HashSet<LoginType>();
			if (loginResult.InfoResultPayload.AccountInfo.GameCenterInfo != null || loginResult.InfoResultPayload.AccountInfo.GooglePlayGamesInfo != null)
			{
				logins.Add(LoginType.Native);
			}
			if (hasEmail)
			{
				logins.Add(LoginType.EmailPass);
			}

			if (!string.IsNullOrEmpty(loginResult.InfoResultPayload.AccountInfo.CustomIdInfo?.CustomId))
			{
				logins.Add(LoginType.LegacyDevice);
			}
			LoginTypes = logins;
			_sessionData.Email = loginResult.InfoResultPayload.AccountInfo.PrivateInfo.Email;
			RawLocalPlayerName = loginResult.InfoResultPayload.PlayerProfile?.DisplayName;
			var initializePlayerAndFetchState = InitializeServerLogicAndFetchState().Preserve();
			var tasks = new List<UniTask>()
			{
				initializePlayerAndFetchState
			};
			tasks.AddRange(_authenticationHooks.Select(a => a.AfterAuthentication(loginResult, previouslyLoggedIn)));
			await UniTask.WhenAll(tasks);

			var serverState = await initializePlayerAndFetchState;
			UpdatePlayerDataAndLogic(serverState, previouslyLoggedIn);
			// Check for account deleation
			if (_dataService.GetData<PlayerData>().Flags.HasFlag(PlayerFlags.Deleted))
			{
				throw new AuthenticationException("This account is deleted!", false);
			}

			await UniTask.WhenAll(_authenticationHooks.Select(a => a.AfterFetchedState(loginResult)));

			_sessionData.IsAuthenticated = true;
			_msgBroker.Publish(new SuccessfullyAuthenticated()
			{
				SessionData = _sessionData,
				PreviouslyLoggedIn = previouslyLoggedIn
			});
		}

		private async UniTask<ServerState> InitializeServerLogicAndFetchState()
		{
			await InitializeServerSidePlayer();
			return await _backendService.FetchServerState();
		}

		public async UniTask<string> SetDisplayName(string newName)
		{
			var newNameTrimmed = newName.Trim();

			string errorMessage = null;
			if (newNameTrimmed.Length < GameConstants.PlayerName.PLAYER_NAME_MIN_LENGTH)
			{
				errorMessage = string.Format(ScriptLocalization.UITProfileScreen.username_too_short,
					GameConstants.PlayerName.PLAYER_NAME_MIN_LENGTH);
			}
			else if (newNameTrimmed.Length > GameConstants.PlayerName.PLAYER_NAME_MAX_LENGTH)
			{
				errorMessage = string.Format(ScriptLocalization.UITProfileScreen.username_too_long,
					GameConstants.PlayerName.PLAYER_NAME_MAX_LENGTH);
			}
			else if (new Regex("[^a-zA-Z0-9 _-\uA421]+").IsMatch(newNameTrimmed))
			{
				errorMessage = ScriptLocalization.UITProfileScreen.username_invalid_characters;
			}

			if (errorMessage != null)
			{
				return errorMessage;
			}

			if (newNameTrimmed == this.GetPrettyLocalPlayerName(showTags: false))
			{
				return null;
			}

			newNameTrimmed = AuthServiceNameExtensions.ReplaceSpacesForSpecialChar(newNameTrimmed);

			UpdateUserTitleDisplayNameResult playfabResponse;
			try
			{
				playfabResponse = await AsyncPlayfabAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest()
				{
					DisplayName = newNameTrimmed
				});
				RawLocalPlayerName = playfabResponse.DisplayName;
			}
			catch (WrappedPlayFabException ex)
			{
				var description = GetErrorString(ex.Error);
				if (ex.Error.Error == PlayFabErrorCode.ProfaneDisplayName)
				{
					description = ScriptLocalization.UITProfileScreen.username_profanity;
				}

				return description;
			}

			try
			{
				await AuthenticationService.Instance.UpdatePlayerNameAsync(newNameTrimmed);
			}
			catch (RequestFailedException e)
			{
				return $"Error setting player name: {e.Message}";
			}

			_msgBroker.Publish(new DisplayNameChangedMessage()
			{
				NewPlayfabDisplayName = playfabResponse.DisplayName
			});
			return null;
		}

		private string GetErrorString(PlayFabError error)
		{
			var realError = error.ErrorDetails?.Values.FirstOrDefault()?.FirstOrDefault();
			return realError ?? error.ErrorMessage;
		}

		private void UpdatePlayerDataAndLogic(Dictionary<string, string> state, bool previouslyLoggedIn)
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
					_dataService.AddData(type, dataInstance);
				}
				catch (Exception e)
				{
					FLog.Error("Error reading data type " + typeFullName, e);
				}
			}

			if (previouslyLoggedIn)
			{
				_logicInitializer.ReInit();
				_msgBroker.Publish(new ReinitializeMenuViewsMessage());
			}
		}

		/// <summary>
		/// Calls GetPlayerData(misleading name ATM) on server, creating data if not exists and also running initialization commands on the server side
		/// </summary>
		private async UniTask InitializeServerSidePlayer()
		{
			var response = await _backendService.CallGenericFunction(CommandNames.GET_PLAYER_DATA);
			var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(response.FunctionResult.ToString());
			var data = serverResult.Result.Data;
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

		public async UniTask AddUserNamePassword(string email, string username, string password)
		{
			await _loginProvider.AddUserNamePassword(email, username, password);
			_sessionData.IsGuest = false;
			_sessionData.Email = email;
		}

		public UniTask SendAccountRecoveryEmail(string email)
		{
			return _loginProvider.SendAccountRecoveryEmail(email);
		}

		public async UniTask Logout()
		{
			await UniTask.WhenAll(_authenticationHooks.Select(a => a.BeforeLogout()));
			await _loginProvider.Logout();
			AuthenticationService.Instance.SignOut(true);
			_sessionData.IsAuthenticated = false;
			_sessionData.Email = null;
		}

		public async UniTask DeleteAccount()
		{
			await _backendService.CallGenericFunction(CommandNames.REMOVE_PLAYER_DATA);
			await Logout();
		}

		public bool HasDeviceLinked()
		{
			return _loginProvider.CanLoginAutomatically();
		}

		public void RegisterHook(IAuthenticationHook hook)
		{
			_authenticationHooks.Add(hook);
		}

		public static GetPlayerCombinedInfoRequestParams StandardLoginInfoRequestParams =>
			new ()
			{
				GetPlayerProfile = true,
				GetUserAccountInfo = true,
				GetTitleData = true,
				GetPlayerStatistics = true
			};
	}
}