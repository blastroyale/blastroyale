using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.BattlePassViews;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

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
		[SerializeField, Required] private BattlePassSegmentListView _battlePassSegmentListView;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private Queue<Equipment> PendingRewards = new();

		public struct StateData
		{
			public IGameUiService UiService;
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

			_gameDataProvider.BattlePassDataProvider.CurrentLevel.InvokeObserve(OnLevelDataUpdated);
			_gameDataProvider.BattlePassDataProvider.CurrentPoints.InvokeObserve(OnPointsDataUpdated);
			
			CheckEnableRewardClaimButton();
			
			this.LateCoroutineCall(_introAnimationClip.length, () => _battlePassSegmentListView.ScrollToBattlePassLevel());
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			
			_services.MessageBrokerService.Unsubscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);
			
			_gameDataProvider.BattlePassDataProvider.CurrentLevel.StopObserving(OnLevelDataUpdated);
			_gameDataProvider.BattlePassDataProvider.CurrentPoints.StopObserving(OnPointsDataUpdated);
		}

		private void OnBackClicked()
		{
			Data.BackClicked();
		}

		private void CheckEnableRewardClaimButton()
		{
			var rewardsRedeemable = _gameDataProvider.BattlePassDataProvider.IsRedeemable(out _);
			_claimRewardsButton.gameObject.SetActive(rewardsRedeemable);
			_nothingToClaimText.gameObject.SetActive(!rewardsRedeemable);
		}

		private void OnBattlePassLevelUp(BattlePassLevelUpMessage message)
		{
			PendingRewards.Clear();
			
			foreach (var equipment in message.Rewards)
			{
				PendingRewards.Enqueue(equipment);
			}

			TryShowNextReward();
			
			_battlePassSegmentListView.UpdateAllSegments();
			CheckEnableRewardClaimButton();
		}

		private void TryShowNextReward()
		{
			// Keep showing/dismissing the battle pass generic reward dialog recursively, until all have been shown
			if (Data.UiService.HasUiPresenter<BattlepassRewardDialogPresenter>())
			{
				Data.UiService.CloseUi<BattlepassRewardDialogPresenter>();
			}
			
			if (!PendingRewards.TryDequeue(out var reward)) return;

			var data = new BattlepassRewardDialogPresenter.StateData()
			{
				ConfirmClicked = TryShowNextReward,
				Reward = reward
			};
					
			Data.UiService.OpenUiAsync<BattlepassRewardDialogPresenter, BattlepassRewardDialogPresenter.StateData>(data);
		}

		private void OnLevelDataUpdated(uint _, uint level)
		{
			UpdateLevelUi();
		}
		
		private void OnPointsDataUpdated(uint _, uint level)
		{
			_battlePassSegmentListView.UpdateAllSegments();
			CheckEnableRewardClaimButton();
			
			UpdateLevelUi();
		}

		private void UpdateLevelUi()
		{
			var redeemableLevelAndPts = _gameDataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			_currentLevelText.text = string.Format(ScriptLocalization.MainMenu.BattlepassCurrentLevel,(redeemableLevelAndPts.Item1+1).ToString());
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