using System;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Domains.HomeScreen.UI;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Execute battle pass initialization command after authentication
	/// </summary>
	public interface IBattlePassService
	{
		public UniTask<IAPHelpers.BuyProductResult> OpenPurchasePopup(bool realMoney);
		public GameProduct FindProduct(bool realMoney);
		public bool HasPendingPurchase();
	}

	public class BattlePassService : IBattlePassService
	{
		public static readonly string PRODUCT_ID_RM = "com.firstlight.blastroyale.premiumpass";
		private readonly string PRODUCT_ID_INGAME = "com.firstlight.blastroyale.premiumpass.ingame";

		private IGameServices _services => MainInstaller.ResolveServices();
		private readonly IMessageBrokerService _msgBroker;
		private IGameDataProvider _gameDataProvider;
		private bool _hasSeenCurrentSeason => _gameDataProvider.BattlePassDataProvider.HasSeenCurrentSeasonBanner();

		public BattlePassService(IGameDataProvider gameDataProvider, IMessageBrokerService msgBroker, IHomeScreenService homeScreenService)
		{
			_msgBroker = msgBroker;
			_gameDataProvider = gameDataProvider;
			homeScreenService.RegisterNotificationQueueProcessor(ProcessHomeScreenBanner);
		}

		private async UniTask<bool> ProcessHomeScreenBanner(Type arg)
		{
			while (true)
			{
				if (arg != typeof(HomeScreenPresenter)) return false;
				var seen = _hasSeenCurrentSeason;
				if (seen) return false;
				if (_gameDataProvider.PlayerDataProvider.Level.Value < 2)
				{
					return false;
				}

				await UniTask.WaitUntil(() => _services.IAPService.UnityStore.Initialized);
				if (FindProduct(true) == null || FindProduct(false) == null)
				{
					FLog.Warn("Not displaying battlepass banner because didn't found store products");
					return false;
				}

				if (_services.RoomService.InRoom) return false;
				if (_services.MatchmakingService.IsMatchmaking.Value) return false;
				if (HasPendingPurchase()) return false;
				var result = await OpenAndWaitBannerResult();
				if (result == BattlePassSeasonBannerPresenter.ScreenResult.GoToBattlePass)
				{
					MarkSeen();
					_msgBroker.Publish(new NewBattlePassSeasonMessage());
					return true;
				}

				if (result == BattlePassSeasonBannerPresenter.ScreenResult.Close)
				{
					MarkSeen();
					return false;
				}

				if (result is BattlePassSeasonBannerPresenter.ScreenResult.BuyPremiumRealMoney
					or BattlePassSeasonBannerPresenter.ScreenResult.BuyPremiumInGame)
				{
					var purchaseResult = await OpenPurchasePopup(result == BattlePassSeasonBannerPresenter.ScreenResult.BuyPremiumRealMoney);
					if (purchaseResult == IAPHelpers.BuyProductResult.Rewarded)
					{
						MarkSeen();
						_msgBroker.Publish(new NewBattlePassSeasonMessage());
						return true;
					}

					if (purchaseResult == IAPHelpers.BuyProductResult.Deferred)
					{
						MarkSeen();
						return false;
					}

					if (purchaseResult == IAPHelpers.BuyProductResult.ForcePlayerToShop)
					{
						MarkSeen();
						return true;
					}
					if (purchaseResult == IAPHelpers.BuyProductResult.Rejected)
					{
						continue;
					}
					
				}

				return false;
			}
		}

		public void MarkSeen()
		{
			_services.CommandService.ExecuteCommand(new BattlepassMarkSeenBanner());
		}

		private async Task<BattlePassSeasonBannerPresenter.ScreenResult> OpenAndWaitBannerResult()
		{
			var callbackWrapper = new UniTaskCompletionSource<BattlePassSeasonBannerPresenter.ScreenResult>();
			await _services.UIService.OpenScreen<BattlePassSeasonBannerPresenter>(new BattlePassSeasonBannerPresenter.StateData()
			{
				ShowBeginSeason = false,
				OnClose = (v) => callbackWrapper.TrySetResult(v)
			});
			var result = await callbackWrapper.Task;
			return result;
		}

		public GameProduct FindProduct(bool realMoney)
		{
			var catalogId = (realMoney ? PRODUCT_ID_RM : PRODUCT_ID_INGAME);
			var product = _services.IAPService.AvailableProductCategories
				.SelectMany(cat => cat.Products)
				.FirstOrDefault(prd => prd.PlayfabProductConfig.CatalogItem.ItemId == catalogId);
			if (product == null)
			{
				return null;
			}

			return product;
		}

		public bool HasPendingPurchase()
		{
			return _services.IAPService.IsPending(FindProduct(true)) || _services.IAPService.IsPending(FindProduct(false));
		}

		public async UniTask<IAPHelpers.BuyProductResult> OpenPurchasePopup(bool realMoney)
		{
			if (_gameDataProvider.BattlePassDataProvider.HasPurchasedSeason())
			{
				await _services.GenericDialogService.OpenSimpleMessageAndWait(ScriptLocalization.UITBattlePass.popup_already_bought_season_title,
					ScriptLocalization.UITBattlePass.popup_already_bought_season_desc);
				return IAPHelpers.BuyProductResult.Rejected;
			}

			if (HasPendingPurchase())
			{
				await _services.GenericDialogService.OpenSimpleMessageAndWait(ScriptLocalization.UITStore.pending_popup_title,
					ScriptLocalization.UITStore.pending_popup_desc);
				return IAPHelpers.BuyProductResult.Rejected;
			}

			var product = FindProduct(realMoney);
			return await _services.IAPService.BuyProductHandleUI(product, _services.UIService, _services.RewardService,
				_services.GenericDialogService);
		}
	}
}