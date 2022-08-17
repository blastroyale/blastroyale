namespace ServerSDK.Models;

/// <summary>
/// Analytics interface to send player behaviour data for data analysis.
/// </summary>
public interface IServerAnalytics
{
	/// <summary>
	/// Emmits a non-user specific event. This tracks global game behaviour.
	/// </summary>
	void EmitEvent(string eventName, AnalyticsData data);
	
	/// <summary>
	/// Emits a user specific event to track user behaviour.
	/// </summary>
	void EmitUserEvent(string id, string eventName, AnalyticsData data);
}

public class AnalyticsData : Dictionary<string, object> { 

	public AnalyticsData() { }
	public AnalyticsData(Dictionary<string, string> stringDict)
	{
		foreach(var kp in stringDict)
		{
			this[kp.Key] = kp.Value;
		}
	}
}