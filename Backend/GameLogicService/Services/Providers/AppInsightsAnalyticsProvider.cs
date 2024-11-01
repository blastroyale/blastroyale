using FirstLight.Server.SDK.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace GameLogicService.Services.Providers
{
	/// <summary>
	/// Analytics implementation for AppInsights
	/// </summary>
	public class AppInsightsAnalyticsProvider : IAnalyticsProvider
	{
		private TelemetryClient _client;
		private ILogger _log;
		
		public AppInsightsAnalyticsProvider(ILogger log)
		{
			_client = new TelemetryClient(TelemetryConfiguration.CreateDefault());
			_log = log;
		}
		
		public void EmitEvent(string eventName, AnalyticsData data)
		{
			_client.TrackEvent(eventName, data);
		}

		public void EmitUserEvent(string id, string eventName, AnalyticsData data)
		{
			data["playfabid"] = id;
			_client.TrackEvent(eventName, data);
		}
	}
}