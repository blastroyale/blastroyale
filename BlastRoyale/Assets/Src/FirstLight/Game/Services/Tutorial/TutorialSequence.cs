using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.Statechart;

namespace FirstLight.Game.Services.Tutorial
{
	/// <summary>
	/// Controls tutorial sequences.
	/// Holds information like current step
	/// </summary>
	public class MetaTutorialSequence : ITutorialSequence
	{
		private IGameServices _services;
		private HashSet<TutorialClientStep> _completed = new();
		private TutorialClientStep? _initialStep;
		
		public string SectionName { get; set; }
		public int SectionVersion { get; set; } = 1;
		public TutorialClientStep CurrentStep { get; set; }
		public MetaTutorialSequence(IGameServices services, TutorialSection section)
		{
			_services = services;
			SectionName = section.ToString();
		}

		public void Reset()
		{
			CurrentStep = _initialStep.Value;
		}

		public void EnterStep(TutorialClientStep newStep)
		{
			SendCurrentStepCompletedAnalytics();
			CurrentStep = newStep;
			if (!_initialStep.HasValue) _initialStep = CurrentStep;
		}

		public void SendCurrentStepCompletedAnalytics()
		{
			if (!_initialStep.HasValue || _completed.Contains(CurrentStep)) return;
			_completed.Add(CurrentStep);
			_services.AnalyticsService.TutorialCalls.CompleteTutorialStep(this);
		}
	}
}