using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Services;
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
		private readonly IDataService _dataService;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public CoreLoopState(GameLogic gameLogic, IGameServices services, IDataService dataService, IGameUiService uiService, IGameDataProvider dataProvider,
		                 IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_dataProvider = dataProvider;
			_services = services;
			_dataService = dataService;
			_statechartTrigger = statechartTrigger;
			_matchState = new MatchState(gameLogic, services, uiService, assetAdderService, statechartTrigger);
			_mainMenuState = new MainMenuState(services, dataService, uiService, gameLogic, assetAdderService, statechartTrigger);
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var match = stateFactory.Nest("Match");
			var mainMenu = stateFactory.Nest("Main Menu");
			var connectionCheck = stateFactory.Choice("Connection Check");
			var connectionWaitToMenu = stateFactory.State("Connection Wait to Menu");
			
			initial.Transition().Target(connectionCheck);

			connectionCheck.Transition().Condition(IsConnectedAndReady).Target(mainMenu);
			connectionCheck.Transition().Target(connectionWaitToMenu);
			
			connectionWaitToMenu.Event(NetworkState.PhotonMasterConnectedEvent).Target(mainMenu);

			mainMenu.Nest(_mainMenuState.Setup).Target(match);

			match.Nest(_matchState.Setup).Target(connectionCheck);
			
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

