using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.StateMachines;

namespace FirstLight.Game.Services.AnalyticsHelpers
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
			var data = new Dictionary<string, object>
			{
				{"section_name", sequence.SectionName},
				{"section_version", sequence.SectionVersion},
				{"section_step", (int)sequence.CurrentStep},
				{"total_step", (int)sequence.CurrentStep},
				{"step_name", sequence.CurrentStep.ToString()},
			};
			
			FLog.Verbose($"Tutorial step complete analytic sending - {sequence.SectionName} v{sequence.SectionVersion} | Step:{sequence.CurrentStep}");

			_analyticsService.LogEvent(AnalyticsEvents.TutorialStepCompleted, data);
		}
	}
}
