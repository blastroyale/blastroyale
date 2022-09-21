using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Services;
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
	public class StubPlayfabService : IPlayfabService
	{
		public List<string> FunctionsCalled = new();

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

		public void LinkDeviceID(Action successCallback = null, Action<PlayFabError> errorCallback = null)
		{
			
		}

		public void UnlinkDeviceID(Action successCallback = null, Action<PlayFabError> errorCallback = null)
		{

		}

		public void AttachLoginDataToAccount(string email, string password, string displayName, Action<AddUsernamePasswordResult> successCallback = null,
		                                     Action<PlayFabError> errorCallback = null)
		{

		}

		public void GetTitleData(string key, Action<string> result = null)
		{
			
		}

		public void FetchServerState(Action<ServerState> callback = null)
		{
			
		}

		public void HandleError(PlayFabError error)
		{
			
		}
	}

}