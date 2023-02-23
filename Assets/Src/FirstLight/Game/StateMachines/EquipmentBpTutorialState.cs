using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using NUnit.Framework;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	public class EquipmentBpTutorialState : ITutorialSequence
	{
		public static readonly IStatechartEvent ProceedGameplayTutorialEvent = new StatechartEvent("TUTORIAL - Proceed gameplay tutorial event");
		
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;
		
		private IMatchServices _matchServices;
		private TutorialUtilsScreenPresenter _tutorialUtilsUi;
		
		public string SectionName { get; set; }
		public int SectionVersion { get; set; }
		public int CurrentStep { get; set; }
		public int CurrentTotalStep => CurrentStep + TotalStepsBeforeThisSection;
		public string CurrentStepName { get; set; }
		public int TotalStepsBeforeThisSection { get; set; }
		

		public EquipmentBpTutorialState(IGameDataProvider logic, IGameServices services,
										IInternalTutorialService tutorialService,
										Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = logic;
			_tutorialService = tutorialService;
			_statechartTrigger = statechartTrigger;
		}

		public void InitSequenceData()
		{
			SectionName = TutorialSection.BP_EQUIPMENT_GUIDE.ToString();
			SectionVersion = 1;
			CurrentStep = 1;
			CurrentStepName = "TutorialStart";
			TotalStepsBeforeThisSection = 0;
		}

		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var loadTutorialUi = stateFactory.TaskWait("Load tutorial UI");
			var playGame = stateFactory.State("Play game");

			initial.Transition().Target(loadTutorialUi);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);
			initial.OnExit(() => { SendAnalyticsIncrementStep("ClickPlayGame"); });
			
			loadTutorialUi.WaitingFor(OpenTutorialScreens).Target(playGame);
			
			// TEMPORARY FLOW - REAL FLOW WILL HAVE BP REWARDS, AND THEN EQUIPPING EQUIPMENT BEFORE MATCH
			playGame.OnEnter(OnPlayGameEnter);
			playGame.Event(MainMenuState.PlayClickedEvent).Target(final);
			playGame.OnExit(() => { SendAnalyticsIncrementStep("TutorialFinish"); });
			playGame.OnExit(OnPlayGameExit);
			
			final.OnEnter(CloseTutorialScreens);
			final.OnEnter(SendStepAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}

		private async Task OpenTutorialScreens()
		{
			await _services.GameUiService.OpenUiAsync<TutorialUtilsScreenPresenter>();
			_tutorialUtilsUi = _services.GameUiService.GetUi<TutorialUtilsScreenPresenter>();
		}
		
		private async void CloseTutorialScreens()
		{
			// Wait for any anims to finish from before before closing the UI
			await Task.Delay(GameConstants.Tutorial.TUTORIAL_SCREEN_OUTRO_CLOSE_TIME);
			
			_services.GameUiService.CloseUi<TutorialUtilsScreenPresenter>(true);
		}

		private void SubscribeMessages()
		{
		}

		private void UnsubscribeMessages()
		{
		}

		public void SendAnalyticsIncrementStep(string newStepName)
		{
			SendStepAnalytics();

			CurrentStep += 1;
			CurrentStepName = newStepName;
		}

		public void SendStepAnalytics()
		{
			_services.AnalyticsService.TutorialCalls.CompleteTutorialStep(SectionName, SectionVersion, CurrentStep,
				CurrentTotalStep, CurrentStepName);
		}
		
		private void OnPlayGameEnter()
		{
			Debug.LogError("LET'S PLAY A REAL MATCH! CLICK 'PLAY' TO START!");
			
			_tutorialUtilsUi.BlockAround<HomeScreenPresenter>(null, "play-button");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>(null, "play-button");
		}
		
		private void OnPlayGameExit()
		{
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.RemoveHighlight();
		}
	}
}