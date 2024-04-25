using System;
using System.Collections.Generic;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Interface for emitting application specific metrics.
	/// </summary>
	public interface IMetricsService
	{
		/// <summary>
		/// Emit a queryble event with custom dimensions to metrics providers 
		/// </summary>
		void EmitEvent(string eventName, Dictionary<string, string>? data = null);

		/// <summary>
		/// Tracks specific handled failures that still shall be displayed on logs & dashboards.
		/// </summary>
		void EmitException(Exception e, string failure);
		/// <summary>
		/// Track a metric value, this can be used later to aggregate data by time like sum, max,min,avg values. 
		/// </summary>
		public void EmitMetric(string metricName, int value);
	}
}