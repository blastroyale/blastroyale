using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
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

		private Coroutine _csPoolTimerCoroutine;

		public CoreLoopState(GameLogic gameLogic, IGameServices services, IDataService dataService,
		                     IGameUiService uiService, IGameDataProvider dataProvider,
		                     IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_dataProvider = dataProvider;
			_services = services;
			_dataService = dataService;
			_statechartTrigger = statechartTrigger;
			_matchState = new MatchState(gameLogic, services, uiService, assetAdderService, statechartTrigger);
			_mainMenuState = new MainMenuState(services, dataService, uiService, gameLogic, assetAdderService,
			                                   statechartTrigger);
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

			mainMenu.OnEnter(StartResourcePoolTimers);
			mainMenu.Nest(_mainMenuState.Setup).Target(match);

			match.Nest(_matchState.Setup).Target(connectionCheck);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ResourcePoolRestockedMessage>(OnResourcePoolRestockedMessage);
			_services.MessageBrokerService.Subscribe<AwardedResourceFromPoolMessage>(OnAwardedResourceFromPoolMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private bool IsConnectedAndReady()
		{
			return _services.NetworkService.QuantumClient.IsConnectedAndReady;
		}

		private void OnResourcePoolRestockedMessage(ResourcePoolRestockedMessage msg)
		{
			StartResourcePoolTimers();
		}
		
		private void OnAwardedResourceFromPoolMessage(AwardedResourceFromPoolMessage msg)
		{
			StartResourcePoolTimers();
		}

		private void StartResourcePoolTimers()
		{
			if (_csPoolTimerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_csPoolTimerCoroutine);
			}
			
			_csPoolTimerCoroutine = _services.CoroutineService.StartCoroutine(ResourcePoolCsTimerCoroutine());
		}

		private IEnumerator ResourcePoolCsTimerCoroutine()
		{
			var poolToObserve = GameId.CS;
			var currentPoolData = _dataProvider.CurrencyDataProvider.ResourcePools[poolToObserve];
			var poolConfig = _services.ConfigsProvider.GetConfigsList<ResourcePoolConfig>()
			                          .FirstOrDefault(x => x.Id == poolToObserve);

			var nextRestockTime = currentPoolData.LastPoolRestockTime.AddMinutes(poolConfig.RestockIntervalMinutes + 1);

			while (DateTime.UtcNow < nextRestockTime)
			{
				yield return null;
			}

			_services.CommandService.ExecuteCommand(new RestockResourcePoolCommand
			{
				PoolId = GameId.CS,
				PoolConfig = poolConfig,
				ForceRestock = false
			});
		}
	}
}