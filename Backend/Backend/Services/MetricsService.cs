using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using ServerSDK.Models;

namespace Backend.Game.Services;

/// <summary>
/// Implementation where we skip sending any metrics. (e.g metrics are disabled)
/// </summary>
public class NoMetrics : IMetricsService
{
	public void EmitEvent(string metricName)
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
}