namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Interface for emitting application specific metrics.
	/// </summary>
	public interface IMetricsService
	{
		void EmitEvent(string metricName);
	}

}

