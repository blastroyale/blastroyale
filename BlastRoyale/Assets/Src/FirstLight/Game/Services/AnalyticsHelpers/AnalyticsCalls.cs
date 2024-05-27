namespace FirstLight.Game.Services.AnalyticsHelpers
{
	/// <summary>
	/// Analytics base class for helper classes
	/// </summary>
	public abstract class AnalyticsCalls
	{
		protected IAnalyticsService _analyticsService;
		
		protected AnalyticsCalls(IAnalyticsService analyticsService)
		{
			_analyticsService = analyticsService;
		}
	}
}