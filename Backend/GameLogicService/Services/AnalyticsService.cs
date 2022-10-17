using System;
using PlayFab;
using PlayFab.ServerModels;
using FirstLight.Server.SDK.Models;
using Microsoft.Extensions.Logging;

namespace Backend.Game.Services
{
	/// <summary>
	/// Analytics implementation for PlayFab Playstream
	/// </summary>
	public class PlaystreamAnalyticsService : IServerAnalytics
	{
		private ILogger _log;
		public PlaystreamAnalyticsService(ILogger log)
		{
			_log = log;
		}
		
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
			}).ContinueWith(t =>
			{
				if (t.Result.Error != null)
				{
					_log.LogError($"Error sending playstream event {eventName} for player {id}: {t.Result.Error.ErrorMessage}");
				}
			});
		}
	}
}