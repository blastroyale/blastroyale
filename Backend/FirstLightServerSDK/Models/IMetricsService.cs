using System;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Interface for emitting application specific metrics.
	/// </summary>
	public interface IMetricsService
	{
		void EmitEvent(string metricName);

		/// <summary>
		/// Tracks specific handled failures that still shall be displayed on logs & dashboards.
		/// </summary>
		void EmitException(Exception e, string failure);
	}

}

