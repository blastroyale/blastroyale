using FirstLight.Game.Services.Analytics.Events;
using FirstLight.Game.StateMachines;

namespace FirstLight.Game.Services.Analytics
{
	/// <summary>
	/// Analytics calls related to tutorials
	/// </summary>
	public class AnalyticsCallsTutorial : AnalyticsCalls
	{
		public AnalyticsCallsTutorial(IAnalyticsService analyticsService) : base(analyticsService)
		{
		}

		/// <summary>
		/// Logs when player completes a tutorial step
		/// </summary>
		public void CompleteTutorialStep(ITutorialSequence sequence)
		{
			_analyticsService.LogEvent(new TutorialStepCompletedEvent((int) sequence.CurrentStep, sequence.CurrentStep.ToString()));
		}
	}
}