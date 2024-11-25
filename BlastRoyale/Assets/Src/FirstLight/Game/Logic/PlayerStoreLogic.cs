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
		
	}

	
	public interface IPlayerStoreLogic : IPlayerStoreDataProvider
	{
		
		void UpdateLastPlayerPurchase(string CatalogItemId, StoreItemData storeItemData);

		void TryResetTrackedStoreData();

		bool IsPurchasedAllowed(string CatalogItemId, StoreItemData storeItemData);
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

		public void UpdateLastPlayerPurchase(string CatalogItemId, StoreItemData storeItemData)
		{
			
			if (!ShouldTrackProductAfterPurchase(storeItemData)) return;

			TryResetTrackedStoreData();

			var trackedPurchaseData = _trackedPlayerPurchases.SingleOrDefault(tpp => tpp.CatalogItemId.Equals(CatalogItemId));

			if (trackedPurchaseData == null)
			{
				_trackedPlayerPurchases.Add(new StorePurchaseData(
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

			var itemsToRemove = _trackedPlayerPurchases
				.Where(tpp => tpp.ShouldDailyReset && (currentDate - tpp.LastPurchaseTime).TotalDays >= 1)
				.ToList();

			foreach (var item in itemsToRemove)
			{
				_trackedPlayerPurchases.Remove(item);
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