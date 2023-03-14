using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	public class TutorialState
	{
		private static readonly IStatechartEvent _startFirstGameTutorialEvent = new StatechartEvent("TUTORIAL - Start First Game Tutorial");
		private static readonly IStatechartEvent _startEquipmentBpTutorialEvent = new StatechartEvent("TUTORIAL - Start Equipment BP Tutorial");
		
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IInternalTutorialService _tutorialService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly FirstGameTutorialState _firstGameTutorialState;
		private readonly MetaAndMatchTutorialState _metaAndMatchTutorialState;
		public TutorialState(IGameDataProvider logic, IGameServices services, IInternalTutorialService tutorialService,
							 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = logic;
			_tutorialService = tutorialService;
			_statechartTrigger = statechartTrigger;
			_firstGameTutorialState = new FirstGameTutorialState(logic, services, tutorialService, statechartTrigger);
			_metaAndMatchTutorialState = new MetaAndMatchTutorialState(logic, services, tutorialService, statechartTrigger);
		}

		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var loadTutorialUi = stateFactory.TaskWait("Load tutorial UI");
			var idle = stateFactory.State("TUTORIAL - Idle");
			var firstGameTutorial = stateFactory.Nest("TUTORIAL - First Game Tutorial");
			var metaAndMatchTutorial = stateFactory.Nest("TUTORIAL - Equipment BP Tutorial");
			
			initial.Transition().Target(loadTutorialUi);
			initial.OnExit(SubscribeMessages);

			loadTutorialUi.WaitingFor(OpenTutorialScreens).Target(idle);
			
			idle.OnEnter(() => SetCurrentSection(TutorialSection.NONE));
			idle.Event(_startFirstGameTutorialEvent).Target(firstGameTutorial);
			idle.Event(_startEquipmentBpTutorialEvent).Target(metaAndMatchTutorial);
			
			firstGameTutorial.OnEnter(() => SetCurrentSection(TutorialSection.FIRST_GUIDE_MATCH));
			firstGameTutorial.Nest(_firstGameTutorialState.Setup).Target(idle);
			firstGameTutorial.OnExit(() => SendSectionCompleted(TutorialSection.FIRST_GUIDE_MATCH));
			
			metaAndMatchTutorial.OnEnter(() => SetCurrentSection(TutorialSection.META_GUIDE_AND_MATCH));
			metaAndMatchTutorial.Nest(_metaAndMatchTutorialState.Setup).Target(idle);
			metaAndMatchTutorial.OnExit(() => SendSectionCompleted(TutorialSection.META_GUIDE_AND_MATCH));
		}

		private async Task OpenTutorialScreens()
		{
			await _services.GameUiService.OpenUiAsync<TutorialUtilsScreenPresenter>();
			await _services.GameUiService.OpenUiAsync<CharacterDialogScreenPresenter>();
			await _services.GameUiService.OpenUiAsync<GuideHandPresenter>();
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
			_services.MessageBrokerService.Subscribe<RequestStartFirstGameTutorialMessage>(OnRequestStartFirstTutorialMessage);
			_services.MessageBrokerService.Subscribe<RequestStartMetaMatchTutorialMessage>(OnRequestStartMetaMatchTutorialMessage);
		}

		private void OnRequestStartFirstTutorialMessage(RequestStartFirstGameTutorialMessage msg)
		{
			if(_tutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH)) return;

			_statechartTrigger(_startFirstGameTutorialEvent);
		}

		private void OnRequestStartMetaMatchTutorialMessage(RequestStartMetaMatchTutorialMessage msg)
		{
			if(_tutorialService.HasCompletedTutorialSection(TutorialSection.META_GUIDE_AND_MATCH)) return;

			_statechartTrigger(_startEquipmentBpTutorialEvent);
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
		/// Set according to flow using GameConstants.Tutorial.TOTAL_STEPS_X
		/// </summary>
		public int TotalStepsBeforeThisSection { get;}

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