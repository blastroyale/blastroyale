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
	public class MetaAndMatchTutorialState : ITutorialSequence
	{
		// CRITICAL - UPDATE THIS WHEN STEPS ARE CHANGED
		public static readonly int TOTAL_STEPS = 6;
		public static readonly IStatechartEvent ProceedTutorialEvent = new StatechartEvent("TUTORIAL - Proceed tutorial event");
		
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
		public int TotalStepsBeforeThisSection => FirstGameTutorialState.TOTAL_STEPS;
		

		public MetaAndMatchTutorialState(IGameDataProvider logic, IGameServices services,
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
			SectionName = TutorialSection.META_GUIDE_AND_MATCH.ToString();
			SectionVersion = 1;
			CurrentStep = 1;
			CurrentStepName = "TutorialStart";
		}

		/// <summary> 
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var enterName = stateFactory.State("Enter name");
			var battlePass = stateFactory.State("Battle Pass");
			var claimRewards = stateFactory.State("Claim rewards");
			var playGame = stateFactory.State("Play game");
			var createTutorialRoom = stateFactory.State("Join Room");
			var waitSimulationStart = stateFactory.State("WaitSimulationStart");
			
			initial.Transition().Target(enterName);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);
			initial.OnExit(GetTutorialScreenRefs);
			
			enterName.OnEnter(() => { SendAnalyticsIncrementStep("EnterName"); });
			enterName.OnEnter(OnEnterNameEnter);
			enterName.Event(EnterNameState.NameSetEvent).Target(battlePass);
			enterName.OnExit(OnEnterNameExit);
			
			battlePass.OnEnter(() => { SendAnalyticsIncrementStep("BattlePassClick"); });
			battlePass.OnEnter(OnBattlePassEnter);
			battlePass.Event(MainMenuState.BattlePassClickedEvent).Target(claimRewards);
			battlePass.OnExit(OnBattlePassExit);
			
			claimRewards.OnEnter(() => { SendAnalyticsIncrementStep("ClaimRewards"); });
			claimRewards.OnEnter(OnClaimRewardsEnter);
			claimRewards.Event(MainMenuState.BattlePassClickedEvent).Target(playGame);
			claimRewards.OnExit(OnClaimRewardsExit);

			playGame.OnEnter(() => { SendAnalyticsIncrementStep("PlayGameClick"); });
			playGame.OnEnter(OnPlayGameEnter);
			playGame.Event(MainMenuState.PlayClickedEvent).Target(createTutorialRoom);
			
			createTutorialRoom.OnEnter(() => { SendAnalyticsIncrementStep("CreateTutorialRoom"); });
			createTutorialRoom.OnEnter(StartSecondTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitSimulationStart);
			
			waitSimulationStart.OnEnter(() => { SendAnalyticsIncrementStep("WaitSimulationStart"); });
			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(final);
			waitSimulationStart.OnExit(() => { SendAnalyticsIncrementStep("TutorialFinish"); });
			
			final.OnEnter(CloseTutorialUi);
			final.OnEnter(SendStepAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}

		private void StartSecondTutorialMatch()
		{
			CloseTutorialUi();
			_tutorialService.CreateJoinSecondTutorialRoom();
		}

		private void GetTutorialScreenRefs()
		{
			_dialogUi = _services.GameUiService.GetUi<CharacterDialogScreenPresenter>();
			_tutorialUtilsUi = _services.GameUiService.GetUi<TutorialUtilsScreenPresenter>();
		}
		
		private void CloseTutorialUi()
		{
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.RemoveHighlight();
			_dialogUi.HideDialog(CharacterType.Female);
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
		
		private void OnEnterNameExit()
		{
			_tutorialUtilsUi.BlockFullScreen();
		}
		
		private async void OnBattlePassEnter()
		{
			_dialogUi.ContinueDialog("AIGHT, LET'S CLAIM SOME REWARDS", CharacterType.Female, CharacterDialogMoodType.Happy);
			
			// Wait a bit until home screen completely uncovers, and we get BP rewards
			await Task.Delay(GameConstants.Tutorial.TUTORIAL_SCREEN_TRANSITION_TIME_LONG);
			
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<HomeScreenPresenter>("battle-pass-button__holder");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>("battle-pass-button__holder");
		}

		
		private void OnBattlePassExit()
		{
			CloseTutorialUi();
			_tutorialUtilsUi.BlockFullScreen();
		}
		
		private async void OnClaimRewardsEnter()
		{
			// Wait a bit until home screen completely uncovers, and we get BP rewards
			await Task.Delay(GameConstants.Tutorial.TUTORIAL_SCREEN_TRANSITION_TIME_LONG);
			await Task.Delay(GameConstants.Tutorial.TUTORIAL_SCREEN_TRANSITION_TIME_LONG);
			_dialogUi.ShowDialog("CLAIM THIS REWARD IDIOT!", CharacterType.Female, CharacterDialogMoodType.Happy, CharacterDialogPosition.TopLeft);
			
			_tutorialUtilsUi.Unblock();
			_services.GameUiService.GetUi<BattlePassScreenPresenter>().EnableFullScreenClaim(true);
			_tutorialUtilsUi.BlockAround<BattlePassScreenPresenter>("first-reward");
			_tutorialUtilsUi.Highlight<BattlePassScreenPresenter>("first-reward",null, 2f);
		}
		
		private void OnClaimRewardsExit()
		{
			_services.GameUiService.GetUi<BattlePassScreenPresenter>().EnableFullScreenClaim(true);
		}
		
		private async void OnPlayGameEnter()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.lets_play_real_match, CharacterType.Female, CharacterDialogMoodType.Happy);

			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<HomeScreenPresenter>("play-button");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>("play-button");
		}
	}
}