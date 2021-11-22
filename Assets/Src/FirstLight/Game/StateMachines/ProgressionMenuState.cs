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
	public class ProgressionMenuState
	{
		private readonly IStatechartEvent _rewardCollectedClickedEvent = new StatechartEvent("Reward Collected Clicked Event");
		private readonly IStatechartEvent _adventureClickedEvent = new StatechartEvent("Adventure Clicked Event");
		private readonly IStatechartEvent _backButtonClickedEvent = new StatechartEvent("ProgressionMenuState Back Button Clicked Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly CollectLootRewardState _collectLootRewardState;
		
		public ProgressionMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider,
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
		public void Setup(IStateFactory stateFactory, IState finalPlayState)
		{
			var initial = stateFactory.Initial("Initial");
			var progressionMenuState = stateFactory.State("Progression Menu State");
			var collectLootBox = stateFactory.Nest("Collect Loot Box State");
			var claimUnclaimedRewards = stateFactory.Transition("Claim Unclaimed Rewards");
			var leavePlayState = stateFactory.Leave("Leave to Play state");
			var firstTimeRewardsLootCheck = stateFactory.Choice("First Time Rewards Loot Check");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(progressionMenuState);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(OpenProgressionMenuUI);

			progressionMenuState.Event(_backButtonClickedEvent).Target(final);
			progressionMenuState.Event(_rewardCollectedClickedEvent).Target(firstTimeRewardsLootCheck);
			progressionMenuState.Event(_adventureClickedEvent).OnTransition(SendAdventurePlayClickedEvent).Target(leavePlayState);
			
			firstTimeRewardsLootCheck.Transition().Condition(CheckAutoLootBoxes).Target(collectLootBox);
			firstTimeRewardsLootCheck.Transition().Condition(CheckUnclaimedRewards).Target(claimUnclaimedRewards);
			firstTimeRewardsLootCheck.Transition().Target(progressionMenuState);
			
			collectLootBox.OnEnter(CloseProgressionMenuRoadUI);
			collectLootBox.Nest(_collectLootRewardState.Setup).Target(firstTimeRewardsLootCheck);
			collectLootBox.OnExit(OpenProgressionMenuUI);
			
			claimUnclaimedRewards.OnEnter(StartClaimRewards);
			claimUnclaimedRewards.Transition().Target(progressionMenuState);
			
			leavePlayState.Transition().Target(finalPlayState);

			final.OnEnter(CloseProgressionMenuRoadUI);
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_gameDataProvider.AdventureDataProvider.AdventureSelectedId.Observe(OnAdventureSelected);
			_services.MessageBrokerService.Subscribe<AdventureFirstTimeRewardsCollectedMessage>(OnAdventureFirstTimeRewardsCollectedMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_gameDataProvider?.AdventureDataProvider?.AdventureSelectedId?.StopObserving(OnAdventureSelected);
		}

		/// <summary>
		/// Checks to see if we have anything in one of our auto loot boxes, which are used as a temporary holding place for
		/// boxes that are acquired that do not count toward regular loot box slots. 
		/// </summary>
		private bool CheckAutoLootBoxes()
		{
			var autoLoot = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo().CoreBoxes;

			if (autoLoot.Count > 0)
			{
				var list = autoLoot.ConvertAll(info => info.Data.Id);
				
				_collectLootRewardState.SetLootBoxToOpen(list);
			}
			
			return autoLoot.Count > 0;
		}

		private bool CheckUnclaimedRewards()
		{
			return _gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0;
		}

		private void OpenProgressionMenuUI()
		{
			var data = new ProgressionScreenPresenter.StateData
			{
				OnProgressMenuClosedClicked = () => _statechartTrigger(_backButtonClickedEvent),
			};

			_uiService.OpenUi<ProgressionScreenPresenter, ProgressionScreenPresenter.StateData>(data);
		}

		private void CloseProgressionMenuRoadUI()
		{
			_uiService.CloseUi<ProgressionScreenPresenter>();
		}

		private void OnAdventureSelected(int previousValue, int newValue)
		{
			_statechartTrigger(_adventureClickedEvent);
		}

		private void OnAdventureFirstTimeRewardsCollectedMessage(AdventureFirstTimeRewardsCollectedMessage message)
		{
			_statechartTrigger(_rewardCollectedClickedEvent);
		}
		
		private void StartClaimRewards()
		{
			_services.CommandService.ExecuteCommand(new CollectUnclaimedRewardsCommand());
		}

		private void SendAdventurePlayClickedEvent()
		{
			var selectedAdventure = _gameDataProvider.AdventureDataProvider.AdventureSelectedInfo;
			var dictionary = new Dictionary<string, object> 
			{
				{"adventure_id", selectedAdventure.AdventureData.Id },
				{"adventure_map", selectedAdventure.Config.Map },
				{"adventure_difficulty", selectedAdventure.Config.Difficulty },
				{"adventure_kill_count", selectedAdventure.AdventureData.KillCount },
				{"player_level", _gameDataProvider.PlayerDataProvider.Level.Value},
			};

			_services.AnalyticsService.LogEvent("play_adventure_clicked", dictionary);
		}
	}
}