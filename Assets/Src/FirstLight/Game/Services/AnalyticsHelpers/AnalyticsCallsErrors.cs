using System.Collections.Generic;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	/// <summary>
	/// Analytics helper class regarding error events
	/// </summary>
	public class AnalyticsCallsErrors : AnalyticsCalls
	{
		public enum ErrorType
		{
			Disconnection,
			Session,
			Login
		}
		public AnalyticsCallsErrors(IAnalyticsService analyticsService) : base(analyticsService)
		{
		}

		public void ReportError(ErrorType type, string description)
		{
			var data = new Dictionary<string, object>
			{
				{"type", type.ToString()},
				{"description", description}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.Error, data);
		}
	}
}