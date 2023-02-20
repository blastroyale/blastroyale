using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	public class TutorialState
	{
		public static readonly IStatechartEvent StartFirstGameTutorialEvent = new StatechartEvent("TUTORIAL - Start First Game Tutorial");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IInternalTutorialService _tutorialService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly FirstGameTutorialState _firstGameTutorialState;

		public TutorialState(IGameDataProvider logic, IGameServices services, IInternalTutorialService tutorialService,
							 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = logic;
			_tutorialService = tutorialService;
			_statechartTrigger = statechartTrigger;
			_firstGameTutorialState = new FirstGameTutorialState(logic, services, tutorialService, statechartTrigger);
		}

		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var idle = stateFactory.State("TUTORIAL - Idle");
			var firstGameTutorial = stateFactory.Nest("TUTORIAL - First Game Tutorial");

			initial.Transition().Target(idle);
			initial.OnExit(SubscribeMessages);

			idle.OnEnter(() => SetCurrentSection(TutorialSection.NONE));
			idle.Event(StartFirstGameTutorialEvent).Target(firstGameTutorial);

			firstGameTutorial.OnEnter(() => SetCurrentSection(TutorialSection.FIRST_GUIDE_MATCH));
			firstGameTutorial.Nest(_firstGameTutorialState.Setup).Target(idle);
			firstGameTutorial.OnExit(() => SendSectionCompleted(TutorialSection.FIRST_GUIDE_MATCH));
		}

		private void SetCurrentSection(TutorialSection section)
		{
			_tutorialService.CurrentRunningTutorial.Value = section;
		}
		
		private void SendSectionCompleted(TutorialSection section)
		{
			_tutorialService.CompleteTutorialSection(section);
		}

		private void SubscribeMessages()
		{
		}
	}

	public interface ITutorialSequence
	{
		/// <summary>
		/// Name of the tutorial section
		/// </summary>
		public string SectionName { get; set; }
		
		/// <summary>
		/// Current iteration for the tutorial section (manually set, CRITICAL for analytics)
		/// </summary>
		public int SectionVersion { get; set; }
		
		/// <summary>
		/// Current step in the tutorial sequence
		/// </summary>
		public int CurrentStep { get; set; }
		
		/// <summary>
		/// Current step in tutorial section, plus all TotalStepsBeforeThisSection
		/// </summary>
		public int CurrentTotalStep { get;}
		
		/// <summary>
		/// Name of the current step in the tutorial sequence
		/// </summary>
		public string CurrentStepName { get; set; }
		
		/// <summary>
		/// Amount of tutorial steps, from all sections, before this tutorial sequence (manually set, CRITICAL for analytics)
		/// </summary>
		public int TotalStepsBeforeThisSection { get; set; }

		/// <summary>
		/// Initialised preliminary data values before anything else in the tutorial sequence
		/// </summary>
		public void InitSequenceData();

		/// <summary>
		/// Sends current step analytics, and updates to a new step
		/// </summary>
		public void SendAnalyticsIncrementStep(string newStepName);
		
		/// <summary>
		/// Sends current step analytics only
		/// </summary>
		public void SendStepAnalytics();
	}
}