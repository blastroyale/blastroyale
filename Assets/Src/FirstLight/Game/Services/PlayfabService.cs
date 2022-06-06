using System;
using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;

namespace FirstLight.Game.Services
{

	/// <summary>
	/// This service handles general interaction with playfab that are not needed by the server
	/// </summary>
	public interface IPlayfabService
	{
		/// <summary>
		/// Updates the user nickname in playfab.
		/// </summary>
		void UpdateNickname(string newNickname);

		/// <summary>
		/// Calls the given cloudscript function with the given arguments.
		/// </summary>
		void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess, Action<PlayFabError> onError, object parameter=null);
	}

	/// <inheritdoc cref="IPlayfabService" />
	public class PlayfabService : IPlayfabService
	{
		private IAppLogic _app;
		
		public PlayfabService(IAppLogic app)
		{
			_app = app;
		}
		
		/// <inheritdoc />
		public void UpdateNickname(string newNickname)
		{
			var request = new UpdateUserTitleDisplayNameRequest { DisplayName = newNickname };
			
			PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnResultCallback, GameCommandService.OnPlayFabError);
			
			void OnResultCallback(UpdateUserTitleDisplayNameResult result)
			{
				_app.NicknameId.Value = result.DisplayName;
			}
		}

		/// <inheritdoc />
		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess, Action<PlayFabError> onError, object parameter = null)
		{
			var request = new ExecuteFunctionRequest
			{
				FunctionName = functionName,
				GeneratePlayStreamEvent = true,
				FunctionParameter = parameter,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};
			PlayFabCloudScriptAPI.ExecuteFunction(request, onSuccess, onError);
		}
	}
}