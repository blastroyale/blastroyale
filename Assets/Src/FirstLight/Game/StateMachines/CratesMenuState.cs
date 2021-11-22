using System;
using System.Collections.Generic;
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
	/// This object contains the behaviour logic for the Loot Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class CratesMenuState
	{
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Loot Screen Back Button Clicked Event");
		private readonly IStatechartEvent _collectLootScreenClickedEvent = new StatechartEvent("Equipment Screen Close Clicked Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly CollectLootRewardState _collectLootRewardState;

		public CratesMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider, 
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
			var cratesMenuState = stateFactory.State("Crates Menu State");
			var collectLoot = stateFactory.Nest("Collect Loot Reward State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(cratesMenuState);
			initial.OnExit(SubscribeEvents);
			
			cratesMenuState.OnEnter(OpenCratesMenuUI);
			cratesMenuState.Event(_backButtonClickedEvent).Target(final);
			cratesMenuState.Event(_collectLootScreenClickedEvent).Target(collectLoot);
			cratesMenuState.OnExit(CloseCratesMenuUI);
			
			collectLoot.Nest(_collectLootRewardState.Setup).Target(cratesMenuState);
			collectLoot.OnExit(OpenCratesMenuUI);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			// Nothing to subscribe
		}

		private void UnsubscribeEvents()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OpenCratesMenuUI()
		{
			var data = new CratesScreenPresenter.StateData
			{
				OnCloseClicked = () => _statechartTrigger(_backButtonClickedEvent),
				LootBoxOpenClicked = OnLootBoxOpenClicked
			};

			_uiService.OpenUi<CratesScreenPresenter, CratesScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new CratesScreenOpenedMessage());
		}

		private void CloseCratesMenuUI()
		{
			_uiService.CloseUi<CratesScreenPresenter>();
		}

		private void OnLootBoxOpenClicked(UniqueId lootBoxUniqueId)
		{
			_collectLootRewardState.SetLootBoxToOpen(new List<UniqueId> { lootBoxUniqueId });
			
			_statechartTrigger(_collectLootScreenClickedEvent);
		}
	}
}