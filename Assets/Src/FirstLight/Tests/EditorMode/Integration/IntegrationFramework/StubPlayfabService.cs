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
		
		public void UpdateNickname(string newNickname)
		{
			
		}

		public void GetTopRankLeaderboard(int amountOfEntries, Action<GetLeaderboardResult> onSuccess, Action<PlayFabError> onError = null)
		{

		}

		public void GetNeighborRankLeaderboard(int amountOfEntries, Action<GetLeaderboardAroundPlayerResult> onSuccess, Action<PlayFabError> onError = null)
		{

		}

		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess, Action<PlayFabError> onError = null, object parameter = null)
		{
			FunctionsCalled.Add(functionName);
			onSuccess ?.Invoke(new ExecuteFunctionResult());
		}

		public void LinkDeviceID(Action successCallback, Action<PlayFabError> errorCallback)
		{
			
		}

		public void UnlinkDeviceID(Action successCallback, Action<PlayFabError> errorCallback)
		{

		}

		public void AttachLoginDataToAccount(string email, string password, string displayName, Action<AddUsernamePasswordResult> successCallback,
		                                     Action<PlayFabError> errorCallback)
		{

		}

		public void GetTitleData(string key, Action<string> result)
		{
			
		}

		public void FetchServerState(Action<ServerState> callback)
		{
			
		}

		public void HandleError(PlayFabError error)
		{
			
		}
	}

}