using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Room Join Create Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class RoomJoinCreateMenuState
	{
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Room Join Create Close Button Clicked Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public RoomJoinCreateMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider,
		                               Action<IStatechartEvent> statechartTrigger, IStatechartEvent presenterClosedEvent)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
			_backButtonClickedEvent = presenterClosedEvent;
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var roomJoinCreateState = stateFactory.State("Room Join Create State");
			var final = stateFactory.Final("Final");
			
			initial.Transition().Target(roomJoinCreateState);
			
			roomJoinCreateState.Event(_backButtonClickedEvent).Target(final);
		}
	}

}