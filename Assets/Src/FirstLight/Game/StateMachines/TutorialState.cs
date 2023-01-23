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
			_firstGameTutorialState = new FirstGameTutorialState(logic, services, statechartTrigger);
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

			idle.OnEnter(() => SetCurrentTutorialStep(TutorialStep.NONE));
			idle.Event(StartFirstGameTutorialEvent).OnTransition(() => SetCurrentTutorialStep(TutorialStep.PLAYED_MATCH)).Target(firstGameTutorial);

			firstGameTutorial.Nest(_firstGameTutorialState.Setup).Target(idle);
			firstGameTutorial.OnExit(() => SendTutorialCompleted(TutorialStep.PLAYED_MATCH));
		}

		private void SetCurrentTutorialStep(TutorialStep step)
		{
			_tutorialService.CurrentRunningTutorial.Value = step;
		}
		
		private void SendTutorialCompleted(TutorialStep step)
		{
			_tutorialService.CompleteTutorialStep(step);
		}

		private void SubscribeMessages()
		{
		}
	}
}