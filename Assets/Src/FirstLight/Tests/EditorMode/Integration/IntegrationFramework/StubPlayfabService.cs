using System;
using System.Collections.Generic;
using FirstLight.Game.Services;
using PlayFab;
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

		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess, Action<PlayFabError> onError = null, object parameter = null)
		{
			FunctionsCalled.Add(functionName);
			onSuccess ?.Invoke(new ExecuteFunctionResult());
		}

		public void HandleError(PlayFabError error)
		{
			
		}
	}

}