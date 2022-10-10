using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using FirstLight.Server.SDK.Models;

namespace Backend.Game.Services
{
	/// <summary>
	/// Implementation where we skip sending any metrics. (e.g metrics are disabled)
	/// </summary>
	public class NoMetrics : IMetricsService
	{
		public void EmitEvent(string metricName)
		{
		
		}

		public void EmitException(Exception e, string failure)
		{
			
		}
	}

	/// <summary>
	/// Minimal implementation of metric emission for App Insights
	/// </summary>
	public class AppInsightsMetrics : IMetricsService
	{
		private TelemetryClient _client;

		public AppInsightsMetrics()
		{
			_client = new TelemetryClient(TelemetryConfiguration.CreateDefault());
		}
	
		public void EmitEvent(string metricName)
		{
			_client.TrackEvent(metricName);
		}

		public void EmitException(Exception e, string failure)
		{
			_client.TrackException(e, new Dictionary<string, string>()
			{
				{ "Message", failure }
			});
		}
	}
}

