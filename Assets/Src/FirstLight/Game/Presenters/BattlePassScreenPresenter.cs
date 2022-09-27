using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles the BattlePass screen - displays the current / next level, the progress, and
	/// shows reward popups when you receive them.
	/// </summary>
	public class BattlePassScreenPresenter : AnimatedUiPresenterData<BattlePassScreenPresenter.StateData>
	{
		[SerializeField, Required] private Button _backButton;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private Queue<BattlePassRewardConfig> PendingRewards = new();

		public struct StateData
		{
			public Action BackClicked;
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_backButton.onClick.AddListener(OnBackClicked);
		}

		protected override void OnOpened()
		{
			_services.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);

			_gameDataProvider.BattlePassDataProvider.CurrentLevel.InvokeObserve(RefreshLevelData);
			_gameDataProvider.BattlePassDataProvider.CurrentPoints.InvokeObserve(RefreshPointsData);

			if (_gameDataProvider.BattlePassDataProvider.IsRedeemable(out _))
			{
				_services.CommandService.ExecuteCommand(new RedeemBPPCommand());
			}
		}

		protected override void OnClosed()
		{
			_services.MessageBrokerService.Unsubscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);

			_gameDataProvider.BattlePassDataProvider.CurrentLevel.StopObserving(RefreshLevelData);
			_gameDataProvider.BattlePassDataProvider.CurrentPoints.StopObserving(RefreshPointsData);
		}

		private void OnBackClicked()
		{
			Data.BackClicked();
		}

		private void OnBattlePassLevelUp(BattlePassLevelUpMessage message)
		{
			PendingRewards.Clear();
			foreach (var config in message.Rewards)
			{
				PendingRewards.Enqueue(config);
			}

			TryShowNextReward();
		}

		private void TryShowNextReward()
		{
			var button = new GenericDialogButton()
			{
				ButtonText = "OK",
				ButtonOnClick = TryShowNextReward
			};

			if (PendingRewards.TryDequeue(out var reward))
			{
				_services.GenericDialogService.OpenDialog($"Reward: {reward.Reward.GameId.ToString()}", false, button);
			}
		}

		private void RefreshLevelData(uint _, uint level)
		{
			//_currentLevel.text = level.ToString();

			if (level < _gameDataProvider.BattlePassDataProvider.MaxLevel)
			{
				//_nextLevel.text = (level + 1).ToString();

				//_nextLevelRewards.gameObject.SetActive(true);
				//var nextReward = _gameDataProvider.BattlePassDataProvider.GetRewardForLevel(level + 1);
				//_nextLevelRewards.text =
					//$"Next level reward:\n{nextReward.Reward.GameId.ToString()}, {nextReward.Reward.Rarity.ToString()}";
			}
			else
			{
				//_nextLevel.text = "MAX";
				//_nextLevelRewards.gameObject.SetActive(false);
			}
		}

		private void RefreshPointsData(uint _, uint points)
		{
			var config = _services.ConfigsProvider.GetConfig<BattlePassConfig>();
			var ppl = config.PointsPerLevel;

			//_progressBar.fillAmount = (float) points / ppl;
			//_progressText.text = $"{points}/{ppl}";
		}
	}
}