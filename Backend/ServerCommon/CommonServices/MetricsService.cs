using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Extensions.Logging;

namespace ServerCommon.CommonServices
{
	/// <summary>
	/// Implementation where we skip sending any metrics. (e.g metrics are disabled)
	/// </summary>
	public class NoMetrics : IMetricsService
	{
		public void EmitEvent(string metricName, Dictionary<string, string>? data = null)
		{
		}

		public void EmitException(Exception e, string failure)
		{
		}

		public void EmitMetric(string metricName, int value)
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

		public void EmitEvent(string eventName, Dictionary<string, string>? data)
		{
			_client.TrackEvent(eventName, data);
		}

		public void EmitMetric(string metricName, int value)
		{
			_client.GetMetric(metricName).TrackValue(value);
		}

		public void EmitException(Exception e, string failure)
		{
			_client.TrackException(e, new Dictionary<string, string>()
			{
				{ "CustomMessage", failure }
			});
		}
	}
}