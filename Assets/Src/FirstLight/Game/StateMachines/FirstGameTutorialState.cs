using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	public class FirstGameTutorialState
	{
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IInternalTutorialService _tutorialService;
		
		public FirstGameTutorialState(IGameDataProvider logic, IGameServices services, IInternalTutorialService tutorialService,
							 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = logic;
			_tutorialService = tutorialService;
			_statechartTrigger = statechartTrigger;
		}
		
		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var createTutorialRoom = stateFactory.State("Create tutorial room");
			var waitingForStart  = stateFactory.State("Waiting for match start");
			var playingMatch  = stateFactory.State("Playing tutorial match");
			
			initial.Transition().Target(createTutorialRoom);
			initial.OnExit(SubscribeMessages);
			
			createTutorialRoom.OnEnter(StartFirstTutorialMatch);
			createTutorialRoom.Event(NetworkState.JoinedRoomEvent).Target(waitingForStart);
			
			waitingForStart.Event(GameSimulationState.SimulationStartedEvent).Target(playingMatch);
			
			playingMatch.Event(MatchState.MatchEndedEvent).Target(final);
			playingMatch.Event(MatchState.MatchEndedExitEvent).Target(final);
			playingMatch.Event(MatchState.MatchQuitEvent).Target(final);
		}

		private void StartFirstTutorialMatch()
		{
			_tutorialService.CreateJoinFirstTutorialRoom();
		}

		private void SubscribeMessages()
		{
			
		}
	}
}