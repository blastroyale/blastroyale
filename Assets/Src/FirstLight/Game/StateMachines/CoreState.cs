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
	public class CoreState
	{
		private static readonly IStatechartEvent _testEvent = new StatechartEvent("Core Event");
		
		private readonly MatchState _matchState;
		private readonly MainMenuState _mainMenuState;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public CoreState(GameLogic gameLogic, IGameServices services, IGameUiService uiService, IGameDataProvider dataProvider,
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
			var final = stateFactory.Final("Final");
			var match = stateFactory.Nest("Match");
			var mainMenu = stateFactory.Nest("Main Menu");
			var runningCheckToMatch = stateFactory.Choice("Running Check To Match");
			var runningCheckToMenu = stateFactory.Choice("Running Check To Menu");
			
			initial.Transition().Target(runningCheckToMenu);

			mainMenu.Nest(_mainMenuState.Setup).Target(runningCheckToMatch);
			runningCheckToMatch.Transition().Condition(IsCoreRunning).Target(match);
			runningCheckToMatch.Transition().Target(final);
			
			match.Nest(_matchState.Setup).Target(runningCheckToMenu);
			runningCheckToMatch.Transition().Condition(IsCoreRunning).Target(mainMenu);
			runningCheckToMatch.Transition().Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			
		}

		private void UnsubscribeEvents()
		{
			
		}

		private bool IsCoreRunning()
		{
			return true;
		}
	}
}
