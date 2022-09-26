using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
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
		void UpdateDisplayName(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess = null, Action<PlayFabError> onError = null);

		/// <summary>
		/// Requests current top leaderboard entries
		/// </summary>
		void GetTopRankLeaderboard(int amountOfEntries, Action<GetLeaderboardResult> onSuccess = null,
		                           Action<PlayFabError> onError = null);

		/// <summary>
		/// Requests leaderboard entries around player with ID <paramref name="playfabID"/>
		/// </summary>
		void GetNeighborRankLeaderboard(int amountOfEntries,
		                                Action<GetLeaderboardAroundPlayerResult> onSuccess = null,
		                                Action<PlayFabError> onError = null);

		/// <summary>
		/// Calls the given cloudscript function with the given arguments.
		/// </summary>
		void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess = null,
		                  Action<PlayFabError> onError = null, object parameter = null);

		/// <summary>
		/// Unlinks this device current account
		/// </summary>
		void LinkDeviceID(Action successCallback = null, Action<PlayFabError> errorCallback = null);

		/// <summary>
		/// Unlinks this device current account
		/// </summary>
		void UnlinkDeviceID(Action successCallback = null, Action<PlayFabError> errorCallback = null);

		/// <summary>
		/// Updates anonymous account with provided registration data
		/// </summary>
		void AttachLoginDataToAccount(string email, string password, string displayName,
		                              Action<AddUsernamePasswordResult> successCallback = null,
		                              Action<PlayFabError> errorCallback = null);

		/// <summary>
		/// Reads the specific title data by the given key.
		/// Throws an error if the key was not present.
		/// </summary>
		void GetTitleData(string key, Action<string> callback);

		/// <summary>
		/// Obtains the server state of the logged in player
		/// </summary>
		void FetchServerState(Action<ServerState> callback);

		/// <summary>
		/// Handles when a request errors out on playfab.
		/// </summary>
		void HandleError(PlayFabError error);
	}

	/// <inheritdoc cref="IPlayfabService" />
	public class PlayfabService : IPlayfabService
	{
		private readonly IGameDataProvider _dataProvider;
		private readonly IMessageBrokerService _msgBroker;

		private readonly string _leaderboardLadderName;

		public PlayfabService(IGameDataProvider dataProvider, IMessageBrokerService msgBroker, string leaderboardLadderName)
		{
			_dataProvider = dataProvider;
			_msgBroker = msgBroker;
			_leaderboardLadderName = leaderboardLadderName;
		}

		/// <inheritdoc />
		public void UpdateDisplayName(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess = null, Action<PlayFabError> onError = null)
		{
			var request = new UpdateUserTitleDisplayNameRequest {DisplayName = newNickname};
			PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnSuccess, HandleError);

			void OnSuccess(UpdateUserTitleDisplayNameResult result)
			{
				_dataProvider.AppDataProvider.DisplayName.Value = result.DisplayName;
				onSuccess?.Invoke(result);
			}
		}

		/// <inheritdoc />
		public void GetTopRankLeaderboard(int amountOfEntries,
		                                  Action<GetLeaderboardResult> onSuccess, Action<PlayFabError> onError = null)
		{
			var leaderboardRequest = new GetLeaderboardRequest()
			{
				StatisticName = _leaderboardLadderName,
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
		public void GetNeighborRankLeaderboard(int amountOfEntries,
		                                       Action<GetLeaderboardAroundPlayerResult> onSuccess = null,
		                                       Action<PlayFabError> onError = null)
		{
			var neighborLeaderboardRequest = new GetLeaderboardAroundPlayerRequest()
			{
				StatisticName = _leaderboardLadderName,
				MaxResultsCount = amountOfEntries
			};

			PlayFabClientAPI.GetLeaderboardAroundPlayer(neighborLeaderboardRequest, onSuccess, (error =>
					                                            {
						                                            onError?.Invoke(error);
						                                            HandleError(error);
					                                            }));
		}

		/// <inheritdoc />
		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess = null,
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

			_msgBroker?.Publish(new ServerHttpErrorMessage()
			{
				ErrorCode = (HttpStatusCode) error.HttpCode,
				Message = descriptiveError
			});
		}

		public void FetchServerState(Action<ServerState> callback)
		{
			PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest(), result =>
			{
				callback.Invoke(new ServerState(result.Data
				                               .ToDictionary(entry => entry.Key,
				                                             entry =>
					                                             entry.Value.Value)));
			}, HandleError);
		}

		/// <summary>
		/// Gets an specific internal title key data
		/// </summary>
		public void GetTitleData(string key, Action<string> callback)
		{
			PlayFabClientAPI.GetTitleData(new GetTitleDataRequest() {Keys = new List<string>() {key}}, res =>
			{
				if (!res.Data.TryGetValue(key, out var data))
				{
					data = null;
				}

				callback.Invoke(data);
			}, HandleError);
		}

		/// <inheritdoc />
		public void LinkDeviceID(Action successCallback = null, Action<PlayFabError> errorCallback = null)
		{
#if UNITY_EDITOR
			var link = new LinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};

			PlayFabClientAPI.LinkCustomID(link, _ => OnSuccess(), errorCallback);
#elif UNITY_ANDROID
			var link = new LinkAndroidDeviceIDRequest
			{
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkAndroidDeviceID(link, _ => OnSuccess(), errorCallback);

#elif UNITY_IOS
			var link = new LinkIOSDeviceIDRequest
			{
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkIOSDeviceID(link, _ => OnSuccess(), errorCallback);
#endif
			void OnSuccess()
			{
				successCallback?.Invoke();
				_dataProvider.AppDataProvider.DeviceID.Value = PlayFabSettings.DeviceUniqueIdentifier;
			}
		}

		/// <inheritdoc />
		public void UnlinkDeviceID(Action successCallback = null, Action<PlayFabError> errorCallback = null)
		{
#if UNITY_EDITOR
			var unlinkRequest = new UnlinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier
			};

			PlayFabClientAPI.UnlinkCustomID(unlinkRequest, _ => OnSuccess(), errorCallback);
#elif UNITY_ANDROID
			var unlinkRequest = new UnlinkAndroidDeviceIDRequest
			{
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};
			
			PlayFabClientAPI.UnlinkAndroidDeviceID(unlinkRequest, _ => OnSuccess(), errorCallback);
#elif UNITY_IOS
			var unlinkRequest = new UnlinkIOSDeviceIDRequest
			{
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};

			PlayFabClientAPI.UnlinkIOSDeviceID(unlinkRequest, _ => OnSuccess(), errorCallback);
#endif
			void OnSuccess()
			{
				_dataProvider.AppDataProvider.DeviceID.Value = "";
				successCallback?.Invoke();
			}
		}

		/// <inheritdoc />
		public void AttachLoginDataToAccount(string email, string password, string username,
		                                     Action<AddUsernamePasswordResult> successCallback = null,
		                                     Action<PlayFabError> errorCallback = null)
		{
			var addUsernamePasswordRequest = new AddUsernamePasswordRequest
			{
				Email = email,
				Username = username,
				Password = password
			};

			PlayFabClientAPI.AddUsernamePassword(addUsernamePasswordRequest, OnSuccess, errorCallback);

			void OnSuccess(AddUsernamePasswordResult result)
			{
				_dataProvider.AppDataProvider.LastLoginEmail.Value = email;
				successCallback?.Invoke(result);
			}
		}
	}
}