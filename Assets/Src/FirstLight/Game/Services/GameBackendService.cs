using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public struct BackendEnvironmentData
	{
		public string EnvironmentID;
		public string TitleID;
		public string AppIDRealtime;
		public string RecoveryEmailTemplateID;
	}

	/// <summary>
	/// This service handles general interaction with playfab that are not needed by the server
	/// </summary>
	public interface IGameBackendService
	{
		/// <summary>
		/// Sets up the backend with the correct cloud environment, per platform
		/// </summary>
		void SetupBackendEnvironment();
		
		/// <summary>
		/// Updates the user nickname in playfab.
		/// </summary>
		void UpdateDisplayName(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Requests current top leaderboard entries
		/// </summary>
		void GetTopRankLeaderboard(int amountOfEntries, Action<GetLeaderboardResult> onSuccess,
		                           Action<PlayFabError> onError);

		/// <summary>
		/// Requests leaderboard entries around player with ID <paramref name="playfabID"/>
		/// </summary>
		void GetNeighborRankLeaderboard(int amountOfEntries,
		                                Action<GetLeaderboardAroundPlayerResult> onSuccess,
		                                Action<PlayFabError> onError);

		/// <summary>
		/// Calls the given cloudscript function with the given arguments.
		/// </summary>
		void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess,
		                  Action<PlayFabError> onError, object parameter = null);

		/// <summary>
		/// Reads the specific title data by the given key.
		/// Throws an error if the key was not present.
		/// </summary>
		void GetTitleData(string key, Action<string> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Obtains the server state of the logged in player
		/// </summary>
		void FetchServerState(Action<ServerState> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Compare server with client rewards to check if they match.
		/// Introduced as a workaround due to requiring two synchronous commands
		/// from two different services (Logic Service & Quantum Server)
		/// </summary>
		void CheckIfRewardsMatch(Action<bool> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Obtains all segments the player is in.
		/// Segments are group of players based on metrics which can be used for various things.
		/// </summary>
		void GetPlayerSegments(Action<List<GetSegmentResult>> onSuccess, Action<PlayFabError> onError);
		
		/// <summary>
		/// Requests a data object with compiled info about the current backend environment
		/// </summary>
		BackendEnvironmentData CurrentEnvironmentData { get; }
		
		/// <summary>
		/// Requests to check if the game is currently in maintenance
		/// </summary>
		bool IsGameInMaintenance();

		/// <summary>
		/// Requests to check if the current game version is outdated
		/// </summary>
		bool IsGameOutdated();

		/// <summary>
		/// Requests the title version downloaded at authentication time
		/// </summary>
		string GetTitleVersion();

		/// <summary>
		/// Handles an incoming error. Sends outgoing messages, analytics and calls back
		/// </summary>
		void HandleError(PlayFabError error, Action<PlayFabError> callback, AnalyticsCallsErrors.ErrorType errorType);
	}

	/// <inheritdoc cref="IGameBackendService" />
	public interface IInternalGameBackendService : IGameBackendService
	{
		new BackendEnvironmentData CurrentEnvironmentData { get; set; }
	}

	/// <inheritdoc cref="IGameBackendService" />
	public class GameBackendService : IInternalGameBackendService
	{
		private readonly IGameDataProvider _dataProvider;
		private readonly IDataService _dataService;
		private readonly IGameServices _services;

		private readonly string _leaderboardLadderName;
		
		public BackendEnvironmentData CurrentEnvironmentData { get; set; }

		public GameBackendService(IGameDataProvider dataProvider, IGameServices services, IDataService dataService, string leaderboardLadderName)
		{
			_dataProvider = dataProvider;
			_dataService = dataService;
			_services = services;
			_leaderboardLadderName = leaderboardLadderName;
		}

		public void GetPlayerSegments(Action<List<GetSegmentResult>> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetPlayerSegments(new GetPlayerSegmentsRequest(), r =>
			{
				onSuccess(r.Segments);
			}, e =>
			{
				HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session);
			});
		}

		public void SetupBackendEnvironment()
		{
			var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;
			var appData = _dataService.GetData<AppData>();
			var envData = new BackendEnvironmentData();
		
#if LIVE_SERVER
			envData.EnvironmentID = "live";
			envData.TitleID = "***REMOVED***";
			envData.AppIDRealtime = "***REMOVED***";
			envData.RecoveryEmailTemplateID = "***REMOVED***";
#elif LIVE_TESTNET_SERVER
			envData.EnvironmentID = "live testnet";
			envData.TitleID = "***REMOVED***";
			envData.AppIDRealtime = "81262db7-24a2-4685-b386-65427c73ce9d";
			envData.RecoveryEmailTemplateID = "***REMOVED***";
#elif STAGE_SERVER
			envData.EnvironmentID = "stage";
			envData.TitleID = "***REMOVED***";
			envData.AppIDRealtime = "***REMOVED***";
			envData.RecoveryEmailTemplateID = "***REMOVED***";
#else
			envData.EnvironmentID = "dev";
			envData.TitleID = "***REMOVED***";
			envData.RecoveryEmailTemplateID = "***REMOVED***";
			envData.AppIDRealtime = "***REMOVED***";
#endif

			CurrentEnvironmentData = envData;
			
			PlayFabSettings.TitleId = CurrentEnvironmentData.TitleID;
			quantumSettings.AppSettings.AppIdRealtime = CurrentEnvironmentData.AppIDRealtime;

			if (CurrentEnvironmentData.EnvironmentID != appData.Environment)
			{
				var newData = appData.CopyForNewEnvironment();

				newData.Environment = CurrentEnvironmentData.EnvironmentID;

				_dataService.AddData(newData, true);
				_dataService.SaveData<AppData>();
			}
		}

		/// <inheritdoc />
		public void UpdateDisplayName(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess, Action<PlayFabError> onError)
		{
			var request = new UpdateUserTitleDisplayNameRequest {DisplayName = newNickname};
			PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnSuccess, e =>
			{
				HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session);
			});

			void OnSuccess(UpdateUserTitleDisplayNameResult result)
			{
				_dataProvider.AppDataProvider.DisplayName.Value = result.DisplayName;
				onSuccess?.Invoke(result);
			}
		}

		public void CheckIfRewardsMatch(Action<bool> onSuccess, Action<PlayFabError> onError) 
		{
			PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest()
			{
				Keys = new List<string>() { typeof(PlayerData).FullName }
			}, result =>
			{
				var modelJson = result.Data[typeof(PlayerData).FullName].Value;
				var model = ModelSerializer.Deserialize<PlayerData>(modelJson);
				var serverState = model.UncollectedRewards;
				var clientState = _dataProvider.RewardDataProvider.UnclaimedRewards;
				var inSync = serverState.SequenceEqual(clientState);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				if (!inSync)
				{
					FLog.Error("Client Rewards: "+ModelSerializer.Serialize(clientState));
					FLog.Error("Server Rewards: "+ModelSerializer.Serialize(serverState));
				}
#endif
				onSuccess(inSync);
			}, e =>
			{
				HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session);
			});
		}

		/// <inheritdoc />
		public void GetTopRankLeaderboard(int amountOfEntries,
		                                  Action<GetLeaderboardResult> onSuccess, Action<PlayFabError> onError)
		{
			var leaderboardRequest = new GetLeaderboardRequest()
			{
				StatisticName = _leaderboardLadderName,
				StartPosition = 0,
				MaxResultsCount = amountOfEntries
			};

			PlayFabClientAPI.GetLeaderboard(leaderboardRequest, onSuccess, e =>
			{
				HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session);
			});
		}

		/// <inheritdoc />
		public void GetNeighborRankLeaderboard(int amountOfEntries,
		                                       Action<GetLeaderboardAroundPlayerResult> onSuccess,
		                                       Action<PlayFabError> onError)
		{
			var neighborLeaderboardRequest = new GetLeaderboardAroundPlayerRequest()
			{
				StatisticName = _leaderboardLadderName,
				MaxResultsCount = amountOfEntries
			};

			PlayFabClientAPI.GetLeaderboardAroundPlayer(neighborLeaderboardRequest, onSuccess, e =>
			{
				HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session);
			});
		}

		/// <inheritdoc />
		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess,
		                         Action<PlayFabError> onError, object parameter = null)
		{
			var request = new ExecuteFunctionRequest
			{
				FunctionName = functionName,
				GeneratePlayStreamEvent = true,
				FunctionParameter = parameter,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			PlayFabCloudScriptAPI.ExecuteFunction(request, res =>
			{
				var exception = ExtractException(res);
				if (exception != null)
				{
					throw exception;
				}
				onSuccess(res);
			}, e =>
			{
				HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session);
			});
		}

		/// <summary>
		/// Playfab cannot return error codes by default. So we require to wrap errors inside ok results.
		/// This function unpacks an exception packed with OK result so its visible on client
		/// </summary>
		private Exception ExtractException(ExecuteFunctionResult req)
		{
			var result = req.FunctionResult as JsonObject;
			if (result.TryGetValue("error", out var error) && error != null)
			{
				return new Exception(error.ToString());
			}
			return null;
		}
		
		public void HandleError(PlayFabError error, Action<PlayFabError> callback, AnalyticsCallsErrors.ErrorType errorType)
		{
			var descriptiveError = $"{error.HttpCode} - {error.ErrorMessage} - {JsonConvert.SerializeObject(error.ErrorDetails)}";
			FLog.Error(descriptiveError);

			_services.AnalyticsService.ErrorsCalls.ReportError(errorType, error.ErrorMessage);
			
			_services.MessageBrokerService?.Publish(new ServerHttpErrorMessage()
			{
				ErrorCode = (HttpStatusCode) error.HttpCode,
				Message = descriptiveError
			});
			
			callback?.Invoke(error);
		}

		public void FetchServerState(Action<ServerState> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest(), result =>
			{
				onSuccess.Invoke(new ServerState(result.Data
				                               .ToDictionary(entry => entry.Key,
				                                             entry =>
					                                             entry.Value.Value)));
			}, e =>
			{
				HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session);
			});
		}

		/// <summary>
		/// Gets an specific internal title key data
		/// </summary>
		public void GetTitleData(string key, Action<string> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetTitleData(new GetTitleDataRequest() {Keys = new List<string>() {key}}, res =>
			{
				if (!res.Data.TryGetValue(key, out var data))
				{
					data = null;
				}

				onSuccess.Invoke(data);
			}, e =>
			{
				HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session);
			});
		}

		public bool IsGameInMaintenance()
		{
			var titleData = _dataService.GetData<AppData>().TitleData;
			
			return titleData.TryGetValue(GameConstants.PlayFab.MAINTENANCE_KEY, out var version) && 
				   VersionUtils.IsOutdatedVersion(version);
		}

		public bool IsGameOutdated()
		{
			var titleVersion = GetTitleVersion();
			
			return VersionUtils.IsOutdatedVersion(titleVersion);
		}

		public string GetTitleVersion()
		{
			var titleData = _dataService.GetData<AppData>().TitleData;
			
			if (!titleData.TryGetValue(GameConstants.PlayFab.VERSION_KEY, out var titleVersion))
			{
				throw new Exception($"{GameConstants.PlayFab.VERSION_KEY} not set in title data");
			}

			return titleVersion;
		}
		
		private String UnityDeviceID()
		{
			var id = SystemInfo.deviceUniqueIdentifier;
#if UNITY_EDITOR
			if (ParrelSync.ClonesManager.IsClone())
			{
				id += "_clone_" + ParrelSync.ClonesManager.GetArgument();
			}
#endif
			return id;
		}
	}
}