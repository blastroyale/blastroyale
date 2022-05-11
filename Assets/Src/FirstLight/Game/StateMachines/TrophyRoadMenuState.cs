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
	/// This object contains the behaviour logic for the Trophy Road Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class TrophyRoadMenuState
	{
		private readonly IStatechartEvent _crateClickedEvent = new StatechartEvent("Crate Clicked Event");
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("Trophy Road Back Button Clicked Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		//private readonly CollectLootRewardState _collectLootRewardState;
		
		public TrophyRoadMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider,
		                           Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
			
			//_collectLootRewardState = new CollectLootRewardState(services, statechartTrigger, _gameDataProvider);
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

			initial.Transition().Target(trophyRoadState);
			initial.OnExit(SubscribeEvents);
			
			trophyRoadState.OnEnter(OpenTrophyRoadUI);
			trophyRoadState.Event(_backButtonClickedEvent).Target(final);
			trophyRoadState.Event(_crateClickedEvent).Target(collectLoot);
			trophyRoadState.OnExit(CloseTrophyRoadUI);
			
			//collectLoot.Nest(_collectLootRewardState.Setup).Target(trophyRoadState);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<TrophyRoadRewardCollectedMessage>(OnTrophyRoadRewardCollectedMessage);
		}

		private void UnsubscribeEvents()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OpenTrophyRoadUI()
		{
			var data = new TrophyRoadScreenPresenter.StateData
			{
				OnTrophyRoadClosedClicked = () => _statechartTrigger(_backButtonClickedEvent),
			};

			_uiService.OpenUi<TrophyRoadScreenPresenter, TrophyRoadScreenPresenter.StateData>(data);
		}

		private void CloseTrophyRoadUI()
		{
			_uiService.CloseUi<TrophyRoadScreenPresenter>();
		}
		
		private async void OnTrophyRoadRewardCollectedMessage(TrophyRoadRewardCollectedMessage message)
		{
			if (!message.Reward.HasValue)
			{
				return;
			}

			if (message.Reward.Value.RewardId.IsInGroup(GameIdGroup.LootBox))
			{
				// TODO: Necessary to not go to an infinite loop on the state machine due to a state machine bug
				await Task.Yield();
				
				//_collectLootRewardState.SetLootBoxToOpen(new List<UniqueId> { message.Reward.Value.Value });
				_statechartTrigger(_crateClickedEvent);
				return;
			}
			
			_services.MessageBrokerService.Publish(new PlayUiVfxCommandMessage
			{
				Id = message.Reward.Value.RewardId,
				OriginWorldPosition = _uiService.GetUi<GenericDialogIconPresenter>().IconPosition.position,
				Quantity = (uint) message.Reward.Value.Value
			});
		}
	}

}