using System;
using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
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
		public List<string> FunctionsCalled = new ();

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

		public void HandleError(PlayFabError error, Action<PlayFabError> callback)
		{
		}

		public void HandleUnrecoverableException(Exception ex)
		{
		}

		public void HandleRecoverableException(Exception ex)
		{
		}

		public bool IsDev()
		{
			return true;
		}

		public bool RunsSimulationOnServer()
		{
			return false;
		}

		public bool ForcedEnvironment => false;

		public void SetupBackendEnvironment(FLEnvironment.Definition? force)
		{
		}

		public void UpdateContactEmail(string newEmail, Action<AddOrUpdateContactEmailResult> onSuccess, Action<PlayFabError> onError)
		{
		}

		public void GetPlayerSegments(Action<List<GetSegmentResult>> onSuccess, Action<PlayFabError> onError)
		{
		}

		public void UpdateDisplayNamePlayfab(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess = null,
									  Action<PlayFabError> onError = null)
		{
		}

		public void GetTopRankLeaderboard(int amountOfEntries, Action<GetLeaderboardResult> onSuccess = null, Action<PlayFabError> onError = null)
		{
		}

		public void GetNeighborRankLeaderboard(int amountOfEntries, Action<GetLeaderboardAroundPlayerResult> onSuccess = null,
											   Action<PlayFabError> onError = null)
		{
		}

		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess = null, Action<PlayFabError> onError = null,
								 object parameter = null)
		{
			FunctionsCalled.Add(functionName);
			onSuccess?.Invoke(new ExecuteFunctionResult());
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