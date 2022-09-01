using System;
using System.Net;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Newtonsoft.Json;
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
		/// Requests current top leaderboard entries
		/// </summary>
		void GetTopRankLeaderboard(string leaderboardName, int amountOfEntries, Action<GetLeaderboardResult> onSuccess,
		                           Action<PlayFabError> onError = null);

		/// <summary>
		/// Requests leaderboard entries around player with ID <paramref name="playfabID"/>
		/// </summary>
		void GetNeighborRankLeaderboard(string leaderboardName, int amountOfEntries,
		                                Action<GetLeaderboardAroundPlayerResult> onSuccess,
		                                Action<PlayFabError> onError = null);

		/// <summary>
		/// Calls the given cloudscript function with the given arguments.
		/// </summary>
		void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess,
		                  Action<PlayFabError> onError = null, object parameter = null);

		/// <summary>
		/// Handles when a request errors out on playfab.
		/// </summary>
		void HandleError(PlayFabError error);
	}

	/// <inheritdoc cref="IPlayfabService" />
	public class PlayfabService : IPlayfabService
	{
		private readonly IAppLogic _app;
		private readonly IMessageBrokerService _msgBroker;

		public PlayfabService(IAppLogic app, IMessageBrokerService msgBroker)
		{
			_app = app;
			_msgBroker = msgBroker;
		}

		/// <inheritdoc />
		public void UpdateNickname(string newNickname)
		{
			var request = new UpdateUserTitleDisplayNameRequest {DisplayName = newNickname};
			PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnResultCallback, HandleError);

			void OnResultCallback(UpdateUserTitleDisplayNameResult result)
			{
				_app.NicknameId.Value = result.DisplayName;
			}
		}

		/// <inheritdoc />
		public void GetTopRankLeaderboard(string leaderboardName, int amountOfEntries,
		                                  Action<GetLeaderboardResult> onSuccess, Action<PlayFabError> onError = null)
		{
			var leaderboardRequest = new GetLeaderboardRequest()
			{
				StatisticName = leaderboardName,
				StartPosition = 0,
				MaxResultsCount = amountOfEntries
			};

			PlayFabClientAPI.GetLeaderboard(leaderboardRequest, onSuccess, (error =>
				                                                               {
					                                                               onError?.Invoke(error);
					                                                               HandleError(error);
				                                                               }));
		}

		/// <inheritdoc />
		public void GetNeighborRankLeaderboard(string leaderboardName, int amountOfEntries,
		                                       Action<GetLeaderboardAroundPlayerResult> onSuccess,
		                                       Action<PlayFabError> onError = null)
		{
			var neighborLeaderboardRequest = new GetLeaderboardAroundPlayerRequest()
			{
				StatisticName = leaderboardName,
				MaxResultsCount = amountOfEntries
			};

			PlayFabClientAPI.GetLeaderboardAroundPlayer(neighborLeaderboardRequest, onSuccess, (error =>
					                                            {
						                                            onError?.Invoke(error);
						                                            HandleError(error);
					                                            }));
		}

		/// <inheritdoc />
		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess,
		                         Action<PlayFabError> onError = null, object parameter = null)
		{
			var request = new ExecuteFunctionRequest
			{
				FunctionName = functionName,
				GeneratePlayStreamEvent = true,
				FunctionParameter = parameter,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};
			
			PlayFabCloudScriptAPI.ExecuteFunction(request, onSuccess, onError ?? HandleError);
		}

		public void HandleError(PlayFabError error)
		{
			var descriptiveError = $"{error.HttpCode} - {error.ErrorMessage} - {JsonConvert.SerializeObject(error.ErrorDetails)}";
			FLog.Error(descriptiveError);
			
			_msgBroker.Publish(new ServerHttpError()
			{
				ErrorCode = (HttpStatusCode) error.HttpCode,
				Message = descriptiveError
			});
		}
	}
}