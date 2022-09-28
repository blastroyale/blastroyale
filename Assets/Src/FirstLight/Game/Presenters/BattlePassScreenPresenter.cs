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
using I2.Loc;
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
		[SerializeField, Required] private Button _claimRewardsButton;
		[SerializeField, Required] private TextMeshProUGUI _currentLevelText;
		[SerializeField, Required] private GameObject _nothingToClaimText;
		
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
			_claimRewardsButton.onClick.AddListener(OnClaimRewardsClicked);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			_services.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);

			_gameDataProvider.BattlePassDataProvider.CurrentLevel.InvokeObserve(RefreshLevelData);

			var rewardsRedeemable = _gameDataProvider.BattlePassDataProvider.IsRedeemable(out _);
			_claimRewardsButton.gameObject.SetActive(rewardsRedeemable);
			_nothingToClaimText.gameObject.SetActive(!rewardsRedeemable);
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			
			_services.MessageBrokerService.Unsubscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);
			
			_gameDataProvider.BattlePassDataProvider.CurrentLevel.StopObserving(RefreshLevelData);
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
			_currentLevelText.text = string.Format(ScriptLocalization.MainMenu.BattlepassCurrentLevel,(level+1).ToString());
		}

		private void OnClaimRewardsClicked()
		{
			if (_gameDataProvider.BattlePassDataProvider.IsRedeemable(out _))
			{
				_services.CommandService.ExecuteCommand(new RedeemBPPCommand());
			}
		}
	}
}