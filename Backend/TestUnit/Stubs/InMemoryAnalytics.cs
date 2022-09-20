using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Models;

public struct FiredEvent
{
	public string User;
	public string Name;
	public AnalyticsData Data;
}

/// <summary>
/// Implementation of in-memory tracking of analytics for unit tests.
/// </summary>
public class InMemoryAnalytics : IServerAnalytics
{
	public List<FiredEvent> FiredEvents = new List<FiredEvent>();
	
	public void EmitEvent(string eventName, AnalyticsData data)
	{
		FiredEvents.Add(new FiredEvent() {Name=eventName, Data = data});
	}

	public void EmitUserEvent(string id, string eventName, AnalyticsData data)
	{
		FiredEvents.Add(new FiredEvent() {User = id, Name= eventName, Data = data});
	}
}