using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.UIElements.Kit;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Season change banner
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class BattlePassSeasonBannerPresenter : UIPresenterData<BattlePassSeasonBannerPresenter.StateData>
	{
		public enum ScreenResult
		{
			Close,
			GoToBattlePass,
			BuyPremiumRealMoney,
			BuyPremiumInGame,
		}

		public class StateData
		{
			public Action<ScreenResult> OnClose;
		}

		private IGameServices _services;

		[Q("TimeLeft")] private Label _timeLeft;
		[Q("FinalRewardIcon")] private VisualElement _finalReward;
		[Q("Rewards")] private VisualElement _rewardsContainer;
		[Q("BuyRM")] private KitButton _buyRM;
		[Q("BuyGame")] private KitButton _buyGameCurrency;
		private VisualElement[] _rewards;
		private Cooldown _closeCooldown;
		private IGameDataProvider _dataProvider;
		private ScreenResult _result = ScreenResult.Close;

		private void Awake()
		{
			_services = MainInstaller.ResolveServices();
			_dataProvider = MainInstaller.ResolveData();
		}

		protected override void QueryElements()
		{
			_rewards = _rewardsContainer.Children().Select(r => r.Q("RewardIcon").Required()).ToArray();
			Root.Q<LocalizedButton>("StartButton").Required().clicked += OnClickOpenBP;
			Root.Q<Button>("CloseButton").clicked += () => ClosePopup();
			Root.Q<VisualElement>("Blocker").RegisterCallback<PointerDownEvent>(ClickedOutside);
			_closeCooldown = new Cooldown(TimeSpan.FromSeconds(2));
			_services.IAPService.PurchaseFinished += IAPServiceOnPurchaseFinished;
			InitBuyButtons();
		}

		protected override UniTask OnScreenClose()
		{
			_services.IAPService.PurchaseFinished -= IAPServiceOnPurchaseFinished;
			Data?.OnClose?.Invoke(_result);
			return UniTask.CompletedTask;
		}

		public void InitBuyButtons()
		{
			var realMoney = _services.BattlePassService.FindProduct(true);
			var inGame = _services.BattlePassService.FindProduct(false);
			if (realMoney == null) throw new Exception("Unable to find real money bp product");
			if (inGame == null) throw new Exception("Unable to find in game bp product");
			_buyRM.BtnText = realMoney.UnityIapProduct().metadata.localizedPriceString;
			var ingamePrice = inGame.GetPrice();
			var sprite = CurrencyItemViewModel.GetRichTextIcon(ingamePrice.item);
			_buyGameCurrency.BtnText = ingamePrice.amt + " " + sprite;

			if (_dataProvider.BattlePassDataProvider.HasPurchasedSeason() || _services.BattlePassService.HasPendingPurchase())
			{
				_buyRM.SetDisplay(false);
				_buyGameCurrency.SetDisplay(false);
				return;
			}

			if (!_dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig().Season.EnableIAP)
			{
				_buyRM.SetDisplay(false);
			}

			_buyRM.clicked += () => ClosePopup(ScreenResult.BuyPremiumRealMoney);
			_buyGameCurrency.clicked += () => ClosePopup(ScreenResult.BuyPremiumInGame);
		}

		private void IAPServiceOnPurchaseFinished(string itemId, ItemData data, bool success, IUnityStoreService.PurchaseFailureData reason)
		{
			if (!success) return;
			if (data.TryGetMetadata<UnlockMetadata>(out var metadata) && metadata.Unlock == UnlockSystem.PaidBattlePass)
			{
				ClosePopup();
			}
		}

		private void ClickedOutside(PointerDownEvent evt)
		{
			if (_closeCooldown.IsCooldown()) return;
			ClosePopup();
		}

		private void ClosePopup(ScreenResult result = ScreenResult.Close)
		{
			_result = result;
			_services.UIService.CloseScreen<BattlePassSeasonBannerPresenter>(false).Forget();
		}

		private void OnClickOpenBP()
		{
			var s = MainInstaller.ResolveServices();
			ClosePopup(ScreenResult.GoToBattlePass);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_closeCooldown.Trigger();
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();
			var endsAt = currentSeason.Season.GetEndsAtDateTime();

			_timeLeft.text = (endsAt - DateTime.UtcNow).ToDayAndHours(true);

			var goodies = GetHighlightedManual();
			if (goodies.Count < 4)
			{
				goodies = GetHighlightedAutomatic();
			}

			var best = goodies.Last();
			goodies.Remove(best);
			var lastRewardView = ItemFactory.Legacy(new LegacyItemData()
			{
				RewardId = best.GameId,
				Value = best.Amount
			}).GetViewModel();
			lastRewardView.DrawIcon(_finalReward);

			for (var x = 0; x < 3; x++)
			{
				var item = ItemFactory.Legacy(new LegacyItemData()
				{
					RewardId = goodies[x].GameId,
					Value = goodies[x].Amount
				});
				var itemView = item.GetViewModel();
				itemView.DrawIcon(_rewards[x]);
			}

			return base.OnScreenOpen(reload);
		}

		private List<EquipmentRewardConfig> GetHighlightedManual()
		{
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();

			var highlighted = currentSeason.Season.GetHighlighted();
			var goodies = highlighted.Select(t => data.BattlePassDataProvider.GetRewardConfigs(new[] {t.level}, t.passType).First()).ToList();

			return goodies;
		}

		private List<EquipmentRewardConfig> GetHighlightedAutomatic()
		{
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();
			var type = currentSeason.Season.RemovePaid ? PassType.Free : PassType.Paid;

			var rewards = data.BattlePassDataProvider.GetRewardConfigs(currentSeason.Levels.Select((a, e) => (uint) e + 1), type);
			rewards.Reverse();

			var bestReward = rewards.First();
			rewards.Remove(bestReward);
			var lastRewardView = ItemFactory.Legacy(new LegacyItemData()
			{
				RewardId = bestReward.GameId,
				Value = bestReward.Amount
			}).GetViewModel();
			lastRewardView.DrawIcon(_finalReward);

			var goodies = rewards.Where(ShouldShowcase).ToList();
			goodies.Add(bestReward);
			foreach (var goodie in goodies) rewards.Remove(goodie);
			while (goodies.Count < 3)
			{
				var reward = rewards.First();
				goodies.Add(reward);
				rewards.Remove(reward);
			}

			return goodies;
		}

		private bool ShouldShowcase(EquipmentRewardConfig reward)
		{
			return reward.GameId.IsInGroup(GameIdGroup.Collection);
		}
	}
}