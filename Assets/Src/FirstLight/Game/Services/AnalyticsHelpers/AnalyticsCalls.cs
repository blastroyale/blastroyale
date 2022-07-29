namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public abstract class AnalyticsCalls
	{
		protected IAnalyticsService _analyticsService;
		
		protected AnalyticsCalls(IAnalyticsService analyticsService)
		{
			_analyticsService = analyticsService;
		}
	}
}