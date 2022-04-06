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
		private readonly IStatechartEvent _crateClickedEvent = new StatechartEvent("Crate Clicked Event");
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Trophy Road Back Button Clicked Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly CollectLootRewardState _collectLootRewardState;
		
		public RoomJoinCreateMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider,
		                               Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
			
			_collectLootRewardState = new CollectLootRewardState(services, statechartTrigger, _gameDataProvider);
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var trophyRoadState = stateFactory.State("Loot Menu State");
			var collectLoot = stateFactory.Nest("Collect Loot Menu");
			var final = stateFactory.Final("Final");

			/*initial.Transition().Target(trophyRoadState);
			initial.OnExit(SubscribeEvents);
			
			trophyRoadState.OnEnter(OpenTrophyRoadUI);
			trophyRoadState.Event(_backButtonClickedEvent).Target(final);
			trophyRoadState.Event(_crateClickedEvent).Target(collectLoot);
			trophyRoadState.OnExit(CloseTrophyRoadUI);
			
			collectLoot.Nest(_collectLootRewardState.Setup).Target(trophyRoadState);

			final.OnEnter(UnsubscribeEvents);*/
		}

	}

}