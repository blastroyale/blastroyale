using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

// ReSharper disable once CheckNamespace

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service provides....
	/// </summary>
	public interface IGameStatisticsService
	{
		/// <summary>
		/// 
		/// </summary>
	}
	
	/// <inheritdoc cref="IGameStatisticsService"/>
	public class GameStatisticsService : IGameStatisticsService
	{
		public void TestServerQuery()
		{
			GetPlayerStatistics(PlayFabSettings.staticPlayer.PlayFabId, (result) =>
			{
				foreach (var s in result.PlayerProfile.Statistics)
				{
					Debug.Log($"{s}");
				}
			},
			(err) =>
			{
				Debug.LogWarning($"{err.ToString()}");
			});
		}

		public void GetLocalPlayerStats(Action<GetPlayerStatisticsResult> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), onSuccess, onError);
		}
		
		public void GetPlayerStatistics(string playerId, Action<GetPlayerProfileResult> onSuccess, Action<PlayFabError> onError)
		{
			var request = new GetPlayerProfileRequest
			{
				PlayFabId = playerId,
				ProfileConstraints = new PlayerProfileViewConstraints()
				{
					ShowStatistics = true
				}
			} ;
			
			PlayFabClientAPI.GetPlayerProfile(request, onSuccess,onError);
		}
	}
}