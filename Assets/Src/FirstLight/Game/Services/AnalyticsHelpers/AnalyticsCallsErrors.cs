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
			Session
		}
		public AnalyticsCallsErrors(IAnalyticsService analyticsService) : base(analyticsService)
		{
		}

		public void ReportError(ErrorType type, string description)
		{
			
		}
	}
}