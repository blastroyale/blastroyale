using System;
using FirstLight.Server.SDK.Models;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.ServerModels;

namespace GameLogicService.Services.Providers
{
	/// <summary>
	/// Analytics implementation for PlayFab Playstream
	/// </summary>
	public class PlayfabPlayStreamAnalyticsServiceProvider : IAnalyticsProvider
	{
		private ILogger _log;
		public PlayfabPlayStreamAnalyticsServiceProvider(ILogger log)
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
			_log.LogDebug($"Sending event {eventName} for user {id}");
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