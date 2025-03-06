using System.Linq;
using FirstLight.Server.SDK.Models;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.ServerModels;

namespace GameLogicService.Services.Providers
{
	/// <summary>
	/// Analytics implementation for AppInsights
	/// </summary>
	public class PlayfabAnalyticsProvider : IAnalyticsProvider
	{
		private ILogger _log;

		public PlayfabAnalyticsProvider(ILogger log)
		{
			_log = log;
		}

		public void EmitEvent(string eventName, AnalyticsData data)
		{
			PlayFabServerAPI.WriteTitleEventAsync(new WriteTitleEventRequest()
			{
				EventName = eventName,
				Body = data.ToDictionary(d => d.Key, d => (object) d.Value),
			}).ContinueWith(t =>
			{
				if (t.Result.Error != null) _log.LogError(t.Result.Error.GenerateErrorReport());
			});
		}

		public void EmitUserEvent(string id, string eventName, AnalyticsData data)
		{
			data["player"] = id;
			PlayFabServerAPI.WritePlayerEventAsync(new WriteServerPlayerEventRequest()
			{
				PlayFabId = id,
				EventName = eventName,
				Body = data.ToDictionary(d => d.Key, d => (object) d.Value),
			}).ContinueWith(t =>
			{
				if (t.Result.Error != null) _log.LogError(t.Result.Error.GenerateErrorReport());
			});
		}
	}
}