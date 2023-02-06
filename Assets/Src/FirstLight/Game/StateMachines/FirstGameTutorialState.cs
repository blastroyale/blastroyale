using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	public class FirstGameTutorialState : ITutorialSequence
	{
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;

		public string SectionName { get; set; }
		public int SectionVersion { get; set; }
		public int CurrentStep { get; set; }
		public int CurrentTotalStep => CurrentStep + TotalStepsBeforeThisSection;
		public string CurrentStepName { get; set; }
		public int TotalStepsBeforeThisSection { get; set; }

		public FirstGameTutorialState(IGameDataProvider logic, IGameServices services,
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
			SectionName = TutorialSection.FIRST_GUIDE_MATCH.ToString();
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
			var createTutorialRoom = stateFactory.State("Create tutorial room");
			var waitingForStart = stateFactory.State("Waiting for match start");
			var playingMatch = stateFactory.State("Playing tutorial match");

			initial.Transition().Target(createTutorialRoom);
			initial.OnExit(SubscribeMessages);
			initial.OnExit(InitSequenceData);
			initial.OnExit(() => { SendAnalyticsIncrementStep("CreateTutorialRoom"); });

			createTutorialRoom.OnEnter(StartFirstTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitingForStart);
			createTutorialRoom.OnExit(() => { SendAnalyticsIncrementStep("WaitingForStart"); });

			waitingForStart.Event(GameSimulationState.SimulationStartedEvent).Target(playingMatch);
			waitingForStart.OnExit(() => { SendAnalyticsIncrementStep("PlayingMatch"); });

			playingMatch.Event(MatchState.MatchStateEndingEvent).Target(final);
			playingMatch.OnExit(() => { SendAnalyticsIncrementStep("TutorialEnd"); });
		}

		private void StartFirstTutorialMatch()
		{
			_tutorialService.CreateJoinFirstTutorialRoom();
		}

		public void SendAnalyticsIncrementStep(string newStepName)
		{
			_services.AnalyticsService.TutorialCalls.CompleteTutorialStep(SectionName, SectionVersion, CurrentStep,
				CurrentTotalStep, CurrentStepName);

			CurrentStep += 1;
			CurrentStepName = newStepName;
		}

		private void SubscribeMessages()
		{
		}
	}
}