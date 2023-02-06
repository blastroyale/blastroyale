using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using UnityEngine;
using UnityEngine.Purchasing;

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
		public void CompleteTutorialStep(string sectionName, int sectionVersion, int sectionStep, int totalStep, string stepName)
		{
			var data = new Dictionary<string, object>
			{
				{"section_name", sectionName},
				{"section_version", sectionVersion},
				{"section_step", sectionStep},
				{"total_step", totalStep},
				{"step_name", stepName},
			};
			
			FLog.Verbose($"Tutorial step complete analytic sending - {sectionName} v{sectionVersion} | Step:{sectionStep} {stepName} Total:{totalStep}");

			_analyticsService.LogEvent(AnalyticsEvents.TutorialStepCompleted, data);
		}
	}
}
