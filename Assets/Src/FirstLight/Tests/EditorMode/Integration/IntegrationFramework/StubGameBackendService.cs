using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;

namespace FirstLight.Tests.EditorMode
{
	/// <summary>
	/// If copied to server tests can be re-implement to run commands on server as well and compare final data results
	/// </summary>
	public class StubGameBackendService : IGameBackendService
	{
		public List<string> FunctionsCalled = new();
		public BackendEnvironmentData CurrentEnvironmentData { get; }

		public bool IsGameInMaintenance()
		{
			return false;
		}

		public bool IsGameOutdated()
		{
			return false;
		}

		public string GetTitleVersion()
		{
			return null;
		}

		public void HandleError(PlayFabError error, Action<PlayFabError> callback, AnalyticsCallsErrors.ErrorType errorType)
		{
			
		}

		public void SetupBackendEnvironment()
		{

		}
		
		public void UpdateContactEmail(string newEmail, Action<AddOrUpdateContactEmailResult> onSuccess, Action<PlayFabError> onError)
		{

		}

		public void GetPlayerSegments(Action<List<GetSegmentResult>> onSuccess, Action<PlayFabError> onError)
		{

		}

		public void UpdateDisplayName(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess = null, Action<PlayFabError> onError = null)
		{

		}

		public void GetTopRankLeaderboard(int amountOfEntries, Action<GetLeaderboardResult> onSuccess = null, Action<PlayFabError> onError = null)
		{

		}

		public void GetNeighborRankLeaderboard(int amountOfEntries, Action<GetLeaderboardAroundPlayerResult> onSuccess = null, Action<PlayFabError> onError = null)
		{

		}

		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess = null, Action<PlayFabError> onError = null, object parameter = null)
		{
			FunctionsCalled.Add(functionName);
			onSuccess ?.Invoke(new ExecuteFunctionResult());
		}

		public void GetTitleData(string key, Action<string> onSuccess, Action<PlayFabError> onError)
		{

		}

		public void FetchServerState(Action<ServerState> onSuccess, Action<PlayFabError> onError)
		{

		}

		public void CheckIfRewardsMatch(Action<bool> onSuccess, Action<PlayFabError> onError)
		{

		}
	}

}