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
			var playGame = stateFactory.State("Play game");
			var disconnected = stateFactory.State("Disconnected");
			var createTutorialRoom = stateFactory.State("Join Room");
			var waitSimulationStart = stateFactory.State("WaitSimulationStart");
			
			initial.Transition().Target(enterName);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);
			initial.OnExit(GetTutorialScreenRefs);
			
			// TEMPORARY FLOW - REAL FLOW WILL HAVE BP REWARDS, AND THEN EQUIPPING EQUIPMENT BEFORE MATCH
			enterName.OnEnter(() => { SendAnalyticsIncrementStep("EnterName"); });
			enterName.OnEnter(OnEnterNameEnter);
			enterName.Event(EnterNameState.NameSetEvent).Target(playGame);
			enterName.Event(NetworkState.PhotonCriticalDisconnectedEvent).Target(disconnected);
			enterName.OnExit(OnEnterNameExit);

			playGame.OnEnter(() => { SendAnalyticsIncrementStep("PlayGameClick"); });
			playGame.OnEnter(OnPlayGameEnter);
			playGame.Event(MainMenuState.PlayClickedEvent).Target(createTutorialRoom);
			playGame.Event(NetworkState.PhotonCriticalDisconnectedEvent).Target(disconnected);
			
			createTutorialRoom.OnEnter(() => { SendAnalyticsIncrementStep("CreateTutorialRoom"); });
			createTutorialRoom.OnEnter(CloseTutorialUi);
			createTutorialRoom.OnEnter(StartSecondTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitSimulationStart);
			createTutorialRoom.Event(NetworkState.PhotonCriticalDisconnectedEvent).Target(disconnected);
			
			disconnected.OnEnter(CloseTutorialUi);
			disconnected.Event(NetworkState.PhotonMasterConnectedEvent).Target(enterName);
			disconnected.OnExit(InitSequenceData);
			
			waitSimulationStart.OnEnter(() => { SendAnalyticsIncrementStep("WaitSimulationStart"); });
			waitSimulationStart.Event(GameSimulationState.SimulationStartedEvent).Target(final);
			waitSimulationStart.OnExit(() => { SendAnalyticsIncrementStep("TutorialFinish"); });
			
			final.OnEnter(SendStepAnalytics);
			final.OnEnter(UnsubscribeMessages);
		}

		private void StartSecondTutorialMatch()
		{
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
		
		private async void OnPlayGameEnter()
		{
			_dialogUi.ContinueDialog(ScriptLocalization.UITTutorial.lets_play_real_match, CharacterType.Female, CharacterDialogMoodType.Happy);
			
			await Task.Delay(GameConstants.Tutorial.TUTORIAL_SCREEN_TRANSITION_TIME_LONG);
			
			_tutorialUtilsUi.Unblock();
			_tutorialUtilsUi.BlockAround<HomeScreenPresenter>("play-button");
			_tutorialUtilsUi.Highlight<HomeScreenPresenter>("play-button");
		}
	}
}