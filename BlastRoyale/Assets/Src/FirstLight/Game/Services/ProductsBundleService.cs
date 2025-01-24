using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Domains.HomeScreen.UI;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using I2.Loc;

namespace FirstLight.Game.Services
{
	
	public interface IProductsBundleService
	{
		public UniTask<IAPHelpers.BuyProductResult> OpenBundlePurchasePopup(string bundleId, bool isRealMoneyPurchase);
		public UniTask<IHomeScreenService.ProcessorResult> OpenProductsBundleBanner(string bundleId);
		public bool HasPendingPurchase(string bundleId);
		public void MarkProductsBundleBannerAsSeen(string bundleId);
		public void MarkProductsBundleBannerAsPurchased(string bundleId);
		public GameProductsBundle GetGameProductBundle(string bundleId);
		public DateTime? GetBundlePurchaseTimeExpireAt(string bundleId);
	}

	public class ProductsBundleService : IProductsBundleService
	{
		private readonly IMessageBrokerService _messageBrokerService;
		
		private readonly IGameDataProvider _gameDataProvider;

		private IReadOnlyCollection<GameProductsBundle> _registeredGameProductBundles;

		public ProductsBundleService(IGameDataProvider gameDataProvider, 
									 IMessageBrokerService messageBrokerService,
									 IHomeScreenService homeScreenService)
		{
			_messageBrokerService = messageBrokerService;
			_gameDataProvider = gameDataProvider;
			homeScreenService.RegisterNotificationQueueProcessor(ProcessHomeScreenBanner);
		}
		
		
		private static IGameServices GameServices => MainInstaller.ResolveServices();

		private void LoadProductBundles()
		{
			_registeredGameProductBundles = GameServices.IAPService.AvailableGameProductBundles;
		}

		private async Task<ProductsBundleBannerPresenter.ScreenResult> OpenAndWaitBannerResultForBundle(string bundleId)
		{
			var callbackWrapper = new UniTaskCompletionSource<ProductsBundleBannerPresenter.ScreenResult>();
			await GameServices.UIService.OpenScreen<ProductsBundleBannerPresenter>(new ProductsBundleBannerPresenter.StateData()
			{
				BundleId = bundleId,
				OnClose = (v) => callbackWrapper.TrySetResult(v)
			});

			var result = await callbackWrapper.Task;

			return result;
		}

		//Bundle has PopupNotifications capabilities to show
		private async UniTask<IHomeScreenService.ProcessorResult> ProcessHomeScreenBanner(Type arg)
		{
			
			while (true)
			{
				if (arg != typeof(HomeScreenPresenter)) return IHomeScreenService.ProcessorResult.None;
				if (GameServices.RoomService.InRoom) return IHomeScreenService.ProcessorResult.None;
				if (GameServices.MatchmakingService.IsMatchmaking.Value) return IHomeScreenService.ProcessorResult.None;
				
				if (_gameDataProvider.PlayerDataProvider.Level.Value < 4)
				{
					return IHomeScreenService.ProcessorResult.None;
				}
				
				await UniTask.WaitUntil(() => GameServices.IAPService.UnityStore.Initialized);

				LoadProductBundles();
				
				foreach (var gameProductBundle in _registeredGameProductBundles)
				{
					var bundleId = gameProductBundle.Bundle.PlayfabProductConfig.CatalogItem.ItemId; 

					if (HasPendingPurchase(bundleId) || HasSeenProductsBundle(bundleId)) continue;

					return await OpenProductsBundleBanner(bundleId);
				}
			}
		}

		public async UniTask<IHomeScreenService.ProcessorResult> OpenProductsBundleBanner(string bundleId)
		{
			var result = await OpenAndWaitBannerResultForBundle(bundleId);

			if (result == ProductsBundleBannerPresenter.ScreenResult.Close)
			{
				MarkProductsBundleBannerAsSeen(bundleId);
				return IHomeScreenService.ProcessorResult.None;
			}

			if (result is ProductsBundleBannerPresenter.ScreenResult.BuyBundleRealMoney
					   or ProductsBundleBannerPresenter.ScreenResult.BuyBundleInGame)
			{

				//Bundles Purchase Popup Has different behaviours/flows when bought with RealMoney or InGame currencies
				var purchaseResult = await OpenBundlePurchasePopup(bundleId, result == ProductsBundleBannerPresenter.ScreenResult.BuyBundleRealMoney);
				if (purchaseResult == IAPHelpers.BuyProductResult.Rewarded)
				{
					MarkProductsBundleBannerAsPurchased(bundleId);
					_messageBrokerService.Publish(new PurchasedBundleMessage());
					return IHomeScreenService.ProcessorResult.OpenOriginalScreen;
				}

				if (purchaseResult == IAPHelpers.BuyProductResult.Deferred)
				{
					MarkProductsBundleBannerAsSeen(bundleId);
					return IHomeScreenService.ProcessorResult.None;
				}

				if (purchaseResult == IAPHelpers.BuyProductResult.Rejected)
				{
					return IHomeScreenService.ProcessorResult.None;
				}
			}

			return IHomeScreenService.ProcessorResult.None;
		}

		public async UniTask<IAPHelpers.BuyProductResult> OpenBundlePurchasePopup(string bundleId, bool isRealMoneyPurchase)
		{
			if (_gameDataProvider.PlayerStoreDataProvider.HasPurchasedProductsBundle(bundleId))
			{
				await GameServices.GenericDialogService.OpenSimpleMessageAndWait(
					ScriptLocalization.UITBattlePass.popup_already_bought_season_title,
					ScriptLocalization.UITBattlePass.popup_already_bought_season_desc);
				
				return IAPHelpers.BuyProductResult.Rejected;
			}
			
			if (HasPendingPurchase(bundleId))
			{
				await GameServices.GenericDialogService.OpenSimpleMessageAndWait(ScriptLocalization.UITStore.pending_popup_title,
					ScriptLocalization.UITStore.pending_popup_desc);
				return IAPHelpers.BuyProductResult.Rejected;
			}

			var product = GetGameProductBundle(bundleId);
			
			return await GameServices.IAPService.BuyProductHandleUI(product.Bundle,
																	GameServices.UIService, 
																	GameServices.RewardService, 
																	GameServices.GenericDialogService);
		}

		public bool HasPendingPurchase(string bundleId)
		{
			return GameServices.IAPService.IsPending(GetGameProductBundle(bundleId).Bundle);
		}
		
		public bool HasSeenProductsBundle(string bundleId)
		{
			return _gameDataProvider.PlayerStoreDataProvider.HasSeenProductsBundleBanner(bundleId);
		}

		public DateTime? GetBundlePurchaseTimeExpireAt(string bundleId)
		{
			var bundle = GetGameProductBundle(bundleId);

			if (bundle == null)
			{
				FLog.Error($"Bundle not found with Id {bundleId}, returning UTC Now");
				return null;
			}

			//Time Remaining is based on the first time player has interacted with the Banner
			if (bundle.Bundle.PlayfabProductConfig.StoreItemData.IsTimeLimitedByPlayer)
			{
				var firstTimeBundleHasShow = _gameDataProvider.PlayerStoreDataProvider.GetFirstTimeBundleHasShowToPlayer(bundleId);
				
				if (firstTimeBundleHasShow == null) 
				{
					firstTimeBundleHasShow = GameServices.CommandService.ExecuteCommandWithResult(new MarkProductsBundleFirstAppearedCommand()
					{
						BundleId = bundleId
					});
				}
				
				var playerPurchaseTimeLimit = firstTimeBundleHasShow.Value.AddSeconds(bundle.Bundle.PlayfabProductConfig.StoreItemData.TimeLimitedByPlayerExpiresAfterSeconds);
				var expireAt = DateTime.UtcNow.Add(playerPurchaseTimeLimit.Subtract(DateTime.UtcNow));    
				return expireAt;
			}

			//Global Cooldown for all players
			if (bundle.Bundle.PlayfabProductConfig.StoreItemData.PurchaseExpiresAt != null)
			{
				return bundle.Bundle.PlayfabProductConfig.StoreItemData.PurchaseExpiresAt;
			}

			//Bundle doesn't expire
			return null;
		}
		

		public void MarkProductsBundleBannerAsSeen(string bundleId)
		{
			GameServices.CommandService.ExecuteCommand(new MarkProductsBundleBannerAsSeenCommand()
			{
				BundleId = bundleId
			});
		}
		
		public void MarkProductsBundleBannerAsPurchased(string bundleId)
		{
			GameServices.CommandService.ExecuteCommand(new MarkProductsBundleBannerAsPurchasedCommand()
			{
				BundleId = bundleId
			});
		}


		public GameProductsBundle GetGameProductBundle(string bundleId)
		{
			return GameServices.IAPService.AvailableGameProductBundles.FirstOrDefault(b => b.Name == bundleId);
		}
	}
}