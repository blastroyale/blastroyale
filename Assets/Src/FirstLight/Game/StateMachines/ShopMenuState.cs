using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using FirstLight.Game.Messages;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Trophy Road Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class ShopMenuState
	{
		private readonly IStatechartEvent _rewardCollectedClickedEvent = new StatechartEvent("Reward Collected Clicked Event");
		private readonly IStatechartEvent _corePurchasedEvent = new StatechartEvent("Core Purchased Event");
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Shop Menu State Back Button Clicked Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly CollectLootRewardState _collectLootRewardState;
		
		public ShopMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider,
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
			var shopMenuState = stateFactory.State("Progression Menu State");
			var collectLootBox = stateFactory.Nest("Collect Loot Box State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(shopMenuState);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(OpenShopMenuUI);

			shopMenuState.Event(_backButtonClickedEvent).Target(final);
			shopMenuState.Event(_corePurchasedEvent).Target(collectLootBox);

			collectLootBox.OnEnter(CloseShopMenuUI);
			collectLootBox.Nest(_collectLootRewardState.Setup).Target(shopMenuState);
			collectLootBox.OnExit(OpenShopMenuUI);

			final.OnEnter(CloseShopMenuUI);
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<IapPurchaseSucceededMessage>(OnIapPurchaseSucceededMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		private void OpenShopMenuUI()
		{
			var data = new ShopScreenPresenter.StateData
			{
				OnShopBackButtonClicked = () => _statechartTrigger(_backButtonClickedEvent),
			};

			_uiService.OpenUi<ShopScreenPresenter, ShopScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new ShopScreenOpenedMessage());
		}

		private void CloseShopMenuUI()
		{
			_uiService.CloseUi<ShopScreenPresenter>();
		}

		private void OnIapPurchaseSucceededMessage(IapPurchaseSucceededMessage message)
		{
			if (message.ProductReward.RewardId.IsInGroup(GameIdGroup.CoreBox) )
			{
				_collectLootRewardState.SetLootBoxToOpen(new List<UniqueId> { message.ProductReward.Value });
				_statechartTrigger(_corePurchasedEvent);
			}
		}
	}
}