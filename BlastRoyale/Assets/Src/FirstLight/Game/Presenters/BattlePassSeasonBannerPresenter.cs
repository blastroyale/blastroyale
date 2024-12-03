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
			public bool ShowBeginSeason;
			public Action<ScreenResult> OnClose;
		}

		private IGameServices _services;

		[Q("TimeLeft")] private Label _timeLeft;
		[Q("ButtonsBeginSeason")] private VisualElement _beginSeasonContainer;
		[Q("ButtonsBuy")] private VisualElement _purchaseContainer;

		private KitButton _buyGameCurrency; // Todo
		private LocalizedButton _buyRMButton;

		private VisualElement[] _paidRewards;
		private VisualElement[] _freeRewards;
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
			_freeRewards = Root
				.Q<VisualElement>("RewardsContainerFree")
				.Children().Select(r => r.Q("RewardIcon").Required())
				.ToArray();

			_paidRewards = Root.Q<VisualElement>("RewardsContainerPremium")
				.Children().Select(r => r.Q("RewardIcon").Required())
				.ToArray();

			Root.Q<Button>("CloseButton").clicked += () => ClosePopup();
			Root.Q<VisualElement>("Blocker").RegisterCallback<PointerDownEvent>(ClickedOutside);
			_closeCooldown = new Cooldown(TimeSpan.FromSeconds(2));
			_services.IAPService.PurchaseFinished += IAPServiceOnPurchaseFinished;
			if (Data.ShowBeginSeason)
			{
				SetupBeginSeasonLayout();
			}
			else
			{
				SetupPurchaseLayout();
			}
		}

		private void SetupBeginSeasonLayout()
		{
			_purchaseContainer.SetDisplay(false);
			_beginSeasonContainer.SetDisplay(true);
			Root.Q<LocalizedButton>("StartButton").Required().clicked += OnClickOpenBP;
			var button = _beginSeasonContainer.Q<VisualElement>("PremiumPassButton").Q<LocalizedButton>("PurchaseButton");
			if (_dataProvider.BattlePassDataProvider.HasPurchasedSeason() || _services.BattlePassService.HasPendingPurchase())
			{
				button.parent.SetDisplay(false);
				return;
			}

			SetupButton(button, _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig().Season.EnableIAP);
		}

		private void SetupPurchaseLayout()
		{
			_purchaseContainer.SetDisplay(true);
			_beginSeasonContainer.SetDisplay(false);
			var realMoneyButton = _purchaseContainer.Q<VisualElement>("RealMoneyBuyButton").Q<LocalizedButton>("PurchaseButton");
			var inGameButton = _purchaseContainer.Q<VisualElement>("InGameBuyButton").Q<LocalizedButton>("PurchaseButton");
			var orLabel = _purchaseContainer.Q<VisualElement>("OrLabel");
			if (_dataProvider.BattlePassDataProvider.HasPurchasedSeason() || _services.BattlePassService.HasPendingPurchase())
			{
				_purchaseContainer.SetDisplay(false);
				return;
			}

			SetupButton(inGameButton, false);
			if (_dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig().Season.EnableIAP)
			{
				SetupButton(realMoneyButton, true);
			}
			else
			{
				orLabel.SetDisplay(false);
				realMoneyButton.parent.SetDisplay(false);
			}
		}

		public void SetupButton(LocalizedButton button, bool realMoney)
		{
			var product = _services.BattlePassService.FindProduct(realMoney);

			if (realMoney)
			{
				button.text = "PREMIUM PASS\n" + product.UnityIapProduct().metadata.localizedPriceString;
			}
			else
			{
				var ingamePrice = product.GetPrice();
				var sprite = CurrencyItemViewModel.GetRichTextIcon(ingamePrice.item);
				button.text = "PREMIUM PASS\n" + ingamePrice.amt + " " + sprite;
			}

			button.clicked += () => ClosePopup(realMoney ? ScreenResult.BuyPremiumRealMoney : ScreenResult.BuyPremiumInGame);
		}

		protected override UniTask OnScreenClose()
		{
			_services.IAPService.PurchaseFinished -= IAPServiceOnPurchaseFinished;
			Data?.OnClose?.Invoke(_result);
			return UniTask.CompletedTask;
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

		protected void FillGoodies(PassType type)
		{
			var list = type == PassType.Paid ? _paidRewards : _freeRewards;
			var highlighted = GetHighlightedManual(type);
			if (highlighted.Count < 3)
			{
				highlighted = GetHighlightedAutomatic(type);
			}

			for (var x = 0; x < 3; x++)
			{
				var item = ItemFactory.Legacy(new LegacyItemData()
				{
					RewardId = highlighted[x].GameId,
					Value = highlighted[x].Amount
				});
				var itemView = item.GetViewModel();
				itemView.DrawIcon(list[x]);
			}
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_closeCooldown.Trigger();
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();
			var endsAt = currentSeason.Season.GetEndsAtDateTime();

			_timeLeft.text = (endsAt - DateTime.UtcNow).ToDayAndHours(true);

			FillGoodies(PassType.Paid);
			FillGoodies(PassType.Free);

			return base.OnScreenOpen(reload);
		}

		private List<EquipmentRewardConfig> GetHighlightedManual(PassType type)
		{
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();

			var highlighted = currentSeason.Season.GetHighlighted();
			var goodies = highlighted
				.Where(t => t.passType == type)
				.Select(t => data.BattlePassDataProvider.GetRewardConfigs(new[] {t.level}, t.passType).First()).ToList();

			return goodies;
		}

		private List<EquipmentRewardConfig> GetHighlightedAutomatic(PassType type)
		{
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();

			var rewards = data.BattlePassDataProvider.GetRewardConfigs(currentSeason.Levels.Select((a, e) => (uint) e + 1), type)
				.Where(ShouldShowcase)
				.Reverse();

			return rewards.TakeLast(3).ToList();
		}

		private bool ShouldShowcase(EquipmentRewardConfig reward)
		{
			return reward.GameId.IsInGroup(GameIdGroup.Collection) && !reward.GameId.IsInGroup(GameIdGroup.ProfilePicture);
		}
	}
}