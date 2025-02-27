using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Services;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Friends;
using Unity.Services.Friends.Options;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	public class UnityCloudHook : IAuthenticationHook
	{
		private IDataService _dataService;
		private IGameBackendService _backendService;
		private IGameRemoteConfigProvider _gameRemoteConfigProvider;
		private IMessageBrokerService _msgBroker;

		private HashSet<Func<UniTask>> _postUnityAuthentication = new ();
		internal event Action<string> OnSetPlayfabName;

		/// <summary>
		///  Called just after the unity authentication finished, services are still not initialized
		/// </summary>
		public event Func<UniTask> PostUnityAuthentication
		{
			add => _postUnityAuthentication.Add(value);
			remove => _postUnityAuthentication.Remove(value);
		}

		public UnityCloudHook(IGameBackendService backendService,
							  IDataService dataService,
							  IGameRemoteConfigProvider gameRemoteConfigProvider,
							  IMessageBrokerService msgBroker)
		{
			_backendService = backendService;
			_dataService = dataService;
			_gameRemoteConfigProvider = gameRemoteConfigProvider;
			_msgBroker = msgBroker;
		}

		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false)
		{
			return UniTask.CompletedTask;
		}

		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn)
		{
			return AuthenticateUnityServices(result, previouslyLoggedIn);
		}

		public UniTask AfterFetchedState(LoginResult result)
		{
			return UniTask.CompletedTask;
		}

		private async UniTask AuthenticateUnityServices(LoginResult playfabLoginResult, bool previouslyLoggedIn)
		{
			if (AuthenticationService.Instance.IsSignedIn) // If is already signed sign out, this is used in retries or logging in
			{
				AuthenticationService.Instance.SignOut(previouslyLoggedIn);
			}

			if (AuthenticationService.Instance.SessionTokenExists)
			{
				FLog.Info("UnityCloudAuth", "Session token exists!");
				// This call will sign in the cached player.
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				FLog.Info("UnityCloudAuth", "Cached user sign in succeeded!");
			}
			else
			{
				await FetchTokenAndAuthenticate();
			}

			await PostUnityAuth(playfabLoginResult);
		}

		public async UniTask FetchTokenAndAuthenticate()
		{
			FLog.Info("UnityCloudAuth", "Requesting session token!");
			var res = await _backendService.CallGenericFunction(CommandNames.AUTHENTICATE_UNITY);
			var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(res.FunctionResult.ToString());
			var data = serverResult.Result.Data;
			FLog.Verbose("UnityCloudAuth", $"Session token received: idToken: {data["idToken"]}, sessionToken: {data["sessionToken"]}!");
			AuthenticationService.Instance.ProcessAuthenticationTokens(data["idToken"], data["sessionToken"]);
		}

		private async UniTask PostUnityAuth(LoginResult loginResult)
		{
			var tasks = new List<UniTask>();
			// Add our events 
			tasks.AddRange(_postUnityAuthentication.Select(func => func.Invoke()));
			tasks.Add(_gameRemoteConfigProvider
				.Init()); // This should not be here because of manual IoC would require a lo tof changes so fuckit 
			tasks.Add(CloudSaveService.Instance.SavePlayfabIDAsync(PlayFabSettings.staticPlayer.PlayFabId));
			tasks.Add(AuthenticationService.Instance.GetPlayerNameAsync().AsUniTask());
			tasks.Add(FriendsService.Instance.InitializeAsync(
					new InitializeOptions()
						.WithEvents(true)
						.WithMemberPresence(true)
						.WithMemberProfile(true))
				.AsUniTask());
			await UniTask.WhenAll(tasks);

			// We need this code because, we use Unity Display Name for the friends system, so they have to be on sync
			CheckNamesUpdates(loginResult)
				.ContinueWith(TryToRunNameMigration)
				.Forget();
		}

		private async UniTask CheckNamesUpdates(LoginResult loginResult)
		{
			try
			{
				var playfabName = AuthServiceNameExtensions.TrimPlayfabDisplayName(loginResult.InfoResultPayload?.PlayerProfile?.DisplayName) ?? "";
				var unityName = AuthServiceNameExtensions.TrimUnityDisplayName(AuthenticationService.Instance.PlayerName) ?? "";

				if (string.IsNullOrWhiteSpace(playfabName))
				{
					FLog.Info("Updating playfab name to auto generated unity one" + unityName);
					var playfabResult = await AsyncPlayfabAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest()
					{
						DisplayName = unityName.Length > 25 ? unityName.Substring(0, 25) : unityName
					});
					OnSetPlayfabName?.Invoke(playfabResult.DisplayName);
					return;
				}

				// Handle old user accounts
				if (!playfabName.Equals(unityName))
				{
					FLog.Info($"Updating unity name('{unityName}') to '{playfabName}'");
					await AuthenticationService.Instance.UpdatePlayerNameAsync(playfabName);
				}
			}
			catch (Exception ex)
			{
				FLog.Error("Failed to update name", ex);
			}
		}

		/// <summary>
		/// This runs on players whom last login is before the friends release, this copies the player name to unity cloud save so he
		/// can be found in friend requests
		/// </summary>
		private void TryToRunNameMigration()
		{
			// GOD FORGIVE ME - GOD MIGHT BUT I WONT :L
			var migrationData = _dataService.LoadData<LocalMigrationData>();
			if (!migrationData.RanMigrations.Contains(LocalMigrationData.SYNC_NAME))
			{
				_backendService.CallGenericFunction(CommandNames.SYNC_NAME).Forget();
				migrationData.RanMigrations.Add(LocalMigrationData.SYNC_NAME);
				_dataService.SaveData<LocalMigrationData>();
			}
		}
	}
}