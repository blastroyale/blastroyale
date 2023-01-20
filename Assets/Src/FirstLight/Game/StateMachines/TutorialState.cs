using System;
using System.Collections;
using System.Collections.Generic;
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
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly FirstGameTutorialState _firstGameTutorialState;

		public TutorialState(IGameDataProvider logic, IGameServices services,
							 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = logic;
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

			idle.Event(StartFirstGameTutorialEvent).Target(firstGameTutorial);

			firstGameTutorial.Nest(_firstGameTutorialState.Setup).Target(idle);
		}

		private void SubscribeMessages()
		{
			throw new System.NotImplementedException();
		}
	}
}