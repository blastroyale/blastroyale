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
using I2.Loc;
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
		private CharacterDialogScreenPresenter _dialogUi;
		
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
			var unloadTutorialUi = stateFactory.TaskWait("Unload tutorial UI");
			var enterName = stateFactory.State("Enter name");
			var playGame = stateFactory.State("Play game");

			initial.Transition().Target(loadTutorialUi);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);
			
			loadTutorialUi.OnEnter(() => { SendAnalyticsIncrementStep("LoadTutorialUi"); });
			loadTutorialUi.WaitingFor(OpenTutorialScreens).Target(enterName);
			
			// TEMPORARY FLOW - REAL FLOW WILL HAVE BP REWARDS, AND THEN EQUIPPING EQUIPMENT BEFORE MATCH
			enterName.OnEnter(() => { SendAnalyticsIncrementStep("EnterName"); });
			enterName.OnEnter(OnEnterNameEnter);
			enterName.Event(EnterNameState.NameSetEvent).Target(playGame);

			playGame.OnEnter(() => { SendAnalyticsIncrementStep("PlayGameClick"); });
			playGame.OnEnter(OnPlayGameEnter);
			playGame.Event(MainMenuState.PlayClickedEvent).Target(unloadTutorialUi);
			playGame.OnExit(() => { SendAnalyticsIncrementStep("TutorialFinish"); });
			
			unloadTutorialUi.OnEnter(() => { SendAnalyticsIncrementStep("UnloadTutorialUi"); });
			unloadTutorialUi.WaitingFor(CloseTutorialScreens).Target(final);
			unloadTutorialUi.OnExit(() => { SendAnalyticsIncrementStep("TutorialFinish"); });
			
			final.OnEnter(SendStepAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}

		private async Task OpenTutorialScreens()
		{
			await _services.GameUiService.OpenUiAsync<TutorialUtilsScreenPresenter>();
			await _services.GameUiService.OpenUiAsync<CharacterDialogScreenPresenter>();
			
			_dialogUi = _services.GameUiService.GetUi<CharacterDialogScreenPresenter>();
			_tutorialUtilsUi = _services.GameUiService.GetUi<TutorialUtilsScreenPresenter>();
		}
		
		private async Task CloseTutorialScreens()
		{
			_dialogUi.HideDialog(CharacterType.Female);
			_tutorialUtilsUi.RemoveHighlight();
			_tutorialUtilsUi.Unblock();
			
			// Wait for any anims to finish from before before closing the UI
			await Task.Delay(GameConstants.Tutorial.TUTORIAL_SCREEN_TRANSITION_TIME_LONG);
			await _services.GameUiService.CloseUi<TutorialUtilsScreenPresenter>(true);
			await _services.GameUiService.CloseUi<CharacterDialogScreenPresenter>(true);
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
		
		private void OnEnterNameEnter()
		{
			_dialogUi.ShowDialog(ScriptLocalization.UITTutorial.enter_your_name, CharacterType.Female, CharacterDialogMoodType.Neutral, CharacterDialogPosition.TopLeft);
		}
		
		private async void OnPlayGameEnter()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.lets_play_real_match, CharacterType.Female, CharacterDialogMoodType.Happy);

			await Task.Delay(1000);
			
			_tutorialUtilsUi.BlockAround<HomeScreenPresenter>("play-button");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>("play-button");
		}
	}
}