using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Server.SDK.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
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
			}).AsUniTask().ContinueWith(t =>
			{
				if(t.Error != null) _log.LogError(t.Error.GenerateErrorReport());
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
			}).AsUniTask().ContinueWith(t =>
			{
				if(t.Error != null) _log.LogError(t.Error.GenerateErrorReport());
			});
		}
	}
}