using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Game.Logic
{

	public interface IPlayerStoreDataProvider
	{
		/// <summary>
		/// List last purchases player did (In case Purchases has Cooldown)
		/// </summary>
		List<StorePurchaseData> GetTrackedPlayerPurchases();

		/// <summary>
		/// Check if Player has purchased the bundle already.
		/// </summary>
		bool HasPurchasedProductsBundle(string bundleId);
		
		/// <summary>
		/// Check if the banner has already shown to the Player.
		/// </summary>
		bool HasSeenProductsBundleBanner(string bundleId);

		/// <summary>
		/// If ProductBundle has limited time per player (i.e. expires 5 days after playing saw the banner for the first time)
		/// We use this function to get it, otherwise, player has never seen the Bundle banner before.
		/// </summary>
		DateTime? GetFirstTimeBundleHasShowToPlayer(string bundleId);
	}

	
	public interface IPlayerStoreLogic : IPlayerStoreDataProvider
	{
		
		void UpdateLastPlayerPurchase(string CatalogItemId, StoreItemData storeItemData);

		void TryResetTrackedStoreData();

		bool IsPurchasedAllowed(string CatalogItemId, StoreItemData storeItemData);

		void MarkProductsBundleAsSeen(string bundleId);
		
		void MarkProductsBundleAsPurchased(string bundleId);
		
		DateTime MarkProductsBundleFirstAppeared(string bundleId);
	}

	
	public class PlayerStoreLogic : AbstractBaseLogic<PlayerStoreData>, IPlayerStoreLogic, IGameLogicInitializer
	{

		private IObservableList<StorePurchaseData> _trackedPlayerPurchases;
		public IObservableListReader<StorePurchaseData> TrackedPlayerPurchases => _trackedPlayerPurchases;
		
		public PlayerStoreLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}
		
		public void Init()
		{

			_trackedPlayerPurchases = new ObservableList<StorePurchaseData>(Data.TrackedStorePurchases);
		}

		public void ReInit()
		{
		}

		public List<StorePurchaseData> GetTrackedPlayerPurchases()
		{
			TryResetTrackedStoreData();
			
			return _trackedPlayerPurchases.ToList();
		}

		public bool HasPurchasedProductsBundle(string bundleId)
		{
			var bundle = Data.ProductsBundlePurchases.SingleOrDefault(pbp => pbp.ProductsBundleId == bundleId);
			return bundle?.HasPurchasedProductsBundle ?? false;
		}

		public bool HasSeenProductsBundleBanner(string bundleId)
		{
			var bundle = Data.ProductsBundlePurchases.SingleOrDefault(pbp => pbp.ProductsBundleId == bundleId);
			return bundle?.HasSeenProductsBundleBanner ?? false;
		}

		public DateTime? GetFirstTimeBundleHasShowToPlayer(string bundleId)
		{
			var bundle = Data.ProductsBundlePurchases.SingleOrDefault(pbp => pbp.ProductsBundleId == bundleId);
			return bundle?.ProductsBundleFirstAppearance;
		}

		public DateTime MarkProductsBundleFirstAppeared(string bundleId)
		{
			var bundlePurchaseData = Data.ProductsBundlePurchases.SingleOrDefault(pbp => pbp.ProductsBundleId == bundleId);

			if (bundlePurchaseData == null)
			{
				var firstAppearance = DateTime.UtcNow;
				
				Data.ProductsBundlePurchases.Add(new ProductBundlePurchaseData(bundleId)
				{
					ProductsBundleFirstAppearance = firstAppearance
				});

				return firstAppearance;
			}

			return bundlePurchaseData.ProductsBundleFirstAppearance;
		}
		
		public void MarkProductsBundleAsSeen(string bundleId)
		{
			var bundle = Data.ProductsBundlePurchases.SingleOrDefault(pbp => pbp.ProductsBundleId == bundleId);

			if (bundle == null)
			{
				Data.ProductsBundlePurchases.Add(new ProductBundlePurchaseData(bundleId)
				{
					HasSeenProductsBundleBanner = true
				});
				
				return;
			}
			
			bundle.HasSeenProductsBundleBanner = true;
		}

		public void MarkProductsBundleAsPurchased(string bundleId)
		{
			var bundle = Data.ProductsBundlePurchases.SingleOrDefault(pbp => pbp.ProductsBundleId == bundleId);

			if (bundle == null)
			{
				Data.ProductsBundlePurchases.Add(new ProductBundlePurchaseData(bundleId)
				{
					HasSeenProductsBundleBanner = true,	
					ProductsBundleFirstAppearance = DateTime.UtcNow,
					HasPurchasedProductsBundle = true
				});
				return;
			}
			
			bundle.HasSeenProductsBundleBanner = true;	
			bundle.HasPurchasedProductsBundle = true;
			bundle.HasPurchasedProductsBundleAt = DateTime.UtcNow;
		}

		
		public void UpdateLastPlayerPurchase(string CatalogItemId, StoreItemData storeItemData)
		{
			
			if (!ShouldTrackProductAfterPurchase(storeItemData)) return;

			TryResetTrackedStoreData();

			var trackedPurchaseData = Data.TrackedStorePurchases.SingleOrDefault(tpp => tpp.CatalogItemId.Equals(CatalogItemId));

			if (trackedPurchaseData == null)
			{
				Data.TrackedStorePurchases.Add(new StorePurchaseData(
					CatalogItemId,
					storeItemData.ShouldDailyReset));
				
				return;
			}

			trackedPurchaseData.AmountPurchased += 1;
			trackedPurchaseData.LastPurchaseTime = DateTime.UtcNow;
		}

		// Certain items have a daily reset, allowing players to purchase them again the next day.
		// For example: A player who has reached the maximum purchase limit for BlastBucks today
		// can return the following day to buy more, as the purchase limit resets daily.
		public void TryResetTrackedStoreData()
		{
			var currentDate = DateTime.UtcNow;

			var itemsToRemove = Data.TrackedStorePurchases
				.Where(tpp => tpp.ShouldDailyReset && (currentDate.Date > tpp.LastPurchaseTime.Date))
				.ToList();

			foreach (var item in itemsToRemove)
			{
				Data.TrackedStorePurchases.Remove(item);
			}
		}

		public bool IsPurchasedAllowed(string CatalogItemId, StoreItemData storeItemData)
		{
			var trackedPurchaseData = _trackedPlayerPurchases.SingleOrDefault(tpp => tpp.CatalogItemId.Equals(CatalogItemId));

			if (trackedPurchaseData != null)
			{
				return trackedPurchaseData.AmountPurchased < storeItemData.MaxAmount &&
					(storeItemData.PurchaseCooldown - (DateTime.UtcNow - trackedPurchaseData.LastPurchaseTime).TotalSeconds) <= 0;
			}

			return true;
		}


		// Determine if the product has restrictions that require tracking after purchase.
		// Examples of such restrictions include:
		// - A maximum quantity that can be purchased within a specific timeframe (e.g., daily or lifetime limits).
		// - A cooldown period between consecutive purchases.
		// - Both restrictions applied together.
		private bool ShouldTrackProductAfterPurchase(StoreItemData storeItemData)
		{
			return storeItemData.MaxAmount > 0 || storeItemData.PurchaseCooldown > 0;
		}
		
	}
}