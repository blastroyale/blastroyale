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
	internal interface IAuthenticationHook
	{
		UniTask BeforeAuthentication(bool previouslyLoggedIn);
		UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn);
		UniTask AfterFetchedState(LoginResult result);
	}

	internal interface ILoginProvider : IAuthenticationHook
	{
		public UniTask Logout();
		public UniTask<LoginResult> LoginWithEmailPassword(string email, string password);
		public UniTask<LoginResult> LoginWithDevice();
		public UniTask AddUserNamePassword(string email, string username, string password);
		public UniTask SendAccountRecoveryEmail(string email);
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
		// For a lack of a better place this will be here
		string RawLocalPlayerName { get; }
		UnityCloudHook UnityCloudAuthentication { get; }
		UniTask AutomaticLogin();
		UniTask LoginWithEmail(string email, string password);

		/// <summary>
		/// Set the player display name and return a error message if any happens
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		UniTask<string> SetDisplayName(string name);

		public UniTask AddUserNamePassword(string email, string username, string password);

		public ISessionData SessionData { get; }
		UniTask SendAccountRecoveryEmail(string email);
		UniTask Logout();
		UniTask DeleteAccount();
	}

	public class SessionData : ISessionData
	{
		public bool IsAuthenticated { get; set; }
		public bool IsFirstSession { get; set; }
		public bool IsGuest { get; set; }
		public string Email { get; set; }
	}

	public class AuthService : IAuthService
	{
		private IInternalGameNetworkService _networkService;
		private IGameBackendService _backendService;
		private ILoginProvider _loginProvider;
		private UnityCloudHook _unityCloudHook;
		private readonly IMessageBrokerService _msgBroker;
		private readonly IGameLogicInitializer _logicInitializer;
		private readonly IDataService _dataService;

		public string RawLocalPlayerName { get; private set; }
		public UnityCloudHook UnityCloudAuthentication => _unityCloudHook;
		public ISessionData SessionData => _sessionData;
		private SessionData _sessionData;

		private IAuthenticationHook[] _authenticationHooks;

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
			_networkService = networkService;
			_loginProvider = new PlayfabDeviceLoginProvider();
			_unityCloudHook = new UnityCloudHook(backendService, dataService, gameRemoteConfigProvider, msgBroker);
			_unityCloudHook.OnSetPlayfabName += OnUnityAuthSetPlayfabName;
			_authenticationHooks = new IAuthenticationHook[]
			{
				_loginProvider,
				_unityCloudHook,
				new MigrateTutorialDataHook(dataService, gameCommandService),
				new ParseFeatureFlagsHook(msgBroker),
				new AuthenticatePhotonHook(networkService),
				new UpdateContactEmailHook()
			};
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

		public UniTask AutomaticLogin()
		{
			_sessionData.Email = null;
			_sessionData.IsAuthenticated = false;
			_sessionData.IsFirstSession = false;
			return LoginProcess(_loginProvider.LoginWithDevice(), false);
		}

		public async UniTask LoginWithEmail(string email, string password)
		{
			await LoginProcess(_loginProvider.LoginWithEmailPassword(email, password), true);
		}

		private async UniTask LoginProcess(UniTask<LoginResult> task, bool previouslyLoggedIn = false)
		{
			await UniTask.WhenAll(_authenticationHooks.Select(a => a.BeforeAuthentication(previouslyLoggedIn)));

			var loginResult = await task;
			_sessionData.IsFirstSession = loginResult.NewlyCreated;
			_sessionData.IsGuest = string.IsNullOrWhiteSpace(loginResult.InfoResultPayload.AccountInfo.PrivateInfo.Email);
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
	}
}