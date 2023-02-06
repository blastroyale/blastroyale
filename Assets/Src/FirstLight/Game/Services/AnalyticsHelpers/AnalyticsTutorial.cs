using System.Collections.Generic;
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
		private readonly IGameServices _services;
		
		public AnalyticsCallsTutorial(IAnalyticsService analyticsService, IGameServices services) : base(analyticsService)
		{
			_services = services;
		}
		
		/// <summary>
		/// Logs when the user purchases a product
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
			
			Debug.LogError($"S:{sectionName} v{sectionVersion} | CS:{sectionStep} TS:{totalStep} N:{stepName}");

			_analyticsService.LogEvent(AnalyticsEvents.TutorialStepCompleted, data);
		}
	}
}
