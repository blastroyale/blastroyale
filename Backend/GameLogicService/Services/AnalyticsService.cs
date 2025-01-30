using System.Collections.Generic;
using System.Linq;
using FirstLight.Server.SDK.Models;
using GameLogicService.Services.Providers;
using Microsoft.Extensions.Logging;

namespace GameLogicService.Services;

public class AnalyticsService : IServerAnalytics
{
	private ILogger _log;
	private List<IAnalyticsProvider> _analyticsProviders;
	
	public AnalyticsService(ILogger log, IEnumerable<IAnalyticsProvider> analyticsProviders)
	{
		_log = log;
		_analyticsProviders = analyticsProviders.ToList();
	}
	
	public void EmitEvent(string eventName, AnalyticsData data)
	{
		_analyticsProviders.ForEach(p => p.EmitEvent(eventName, data));
	}
	
	public void EmitUserEvent(string id, string eventName, AnalyticsData data)
	{
		_analyticsProviders.ForEach(p => p.EmitUserEvent(id, eventName, data));
	}
}