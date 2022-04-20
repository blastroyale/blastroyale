using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Network State and communication with Quantum servers in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class CoreLoopState
	{
		private static readonly IStatechartEvent _testEvent = new StatechartEvent("Core Event");
		
		private readonly MatchState _matchState;
		private readonly MainMenuState _mainMenuState;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public CoreLoopState(GameLogic gameLogic, IGameServices services, IGameUiService uiService, IGameDataProvider dataProvider,
		                 IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_dataProvider = dataProvider;
			_services = services;
			_statechartTrigger = statechartTrigger;
			_matchState = new MatchState(gameLogic, services, uiService, assetAdderService, statechartTrigger);
			_mainMenuState = new MainMenuState(services, uiService, gameLogic, assetAdderService, statechartTrigger);
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var initialConnectionCheck = stateFactory.State("Initial Connection Check");
			var final = stateFactory.Final("Final");
			var match = stateFactory.Nest("Match");
			var mainMenu = stateFactory.Nest("Main Menu");
			var connectionCheckToMenu = stateFactory.Choice("Connection Check To Menu");
			var reconnectWaitToMenu = stateFactory.State("Reconnect Attempt To Menu");
			
			initial.Transition().Target(initialConnectionCheck);

			initialConnectionCheck.Event(NetworkState.PhotonMasterConnectedEvent).Target(connectionCheckToMenu);

			mainMenu.Nest(_mainMenuState.Setup).Target(match);

			match.Nest(_matchState.Setup).Target(connectionCheckToMenu);

			connectionCheckToMenu.Transition().Condition(IsConnectedAndReady).Target(mainMenu);
			connectionCheckToMenu.Transition().Target(reconnectWaitToMenu);
			
			reconnectWaitToMenu.Event(NetworkState.PhotonMasterConnectedEvent).Target(mainMenu);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}
		
		private bool IsConnectedAndReady()
		{
			return _services.NetworkService.QuantumClient.IsConnectedAndReady;
		}
	}
}

