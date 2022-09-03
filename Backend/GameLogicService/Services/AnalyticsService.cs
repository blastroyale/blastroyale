using System;
using PlayFab;
using PlayFab.ServerModels;
using FirstLight.Server.SDK.Models;

namespace Backend.Game.Services
{
	/// <summary>
	/// Analytics implementation for PlayFab Playstream
	/// </summary>
	public class PlaystreamAnalyticsService : IServerAnalytics
	{
		public void EmitEvent(string eventName, AnalyticsData data)
		{
			PlayFabServerAPI.WriteTitleEventAsync(new WriteTitleEventRequest()
			{
				Body = data,
				Timestamp = DateTime.UtcNow,
				EventName = eventName
			});
		}

		public void EmitUserEvent(string id, string eventName, AnalyticsData data)
		{
			PlayFabServerAPI.WritePlayerEventAsync(new WriteServerPlayerEventRequest()
			{
				PlayFabId = id,
				Body = data,
				Timestamp = DateTime.UtcNow,
				EventName = eventName
			});
		}
	}
}

