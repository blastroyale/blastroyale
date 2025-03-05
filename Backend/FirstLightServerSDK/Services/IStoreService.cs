using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstLightServerSDK.Services
{
	[Serializable]
	public class FlgStoreItem
	{
		public string ItemId;
		public Dictionary<string, uint> Price;
	}

	[Serializable]
	public class FlgSpecialStoreConfiguration
	{
		public float StoreBaseChanceToShow;
		public float StoreMaxChanceToShow;
		public int MaxItems;
		public int ResetToChanceBaseAfterDays;
		public StoreIncrementChancesConfiguration StoreIncrementChances;
	}
	
	public class StoreIncrementChancesConfiguration
	{
		public float Min;
		public float Max;
	}

	/// <summary>
	/// Represents server setup of the in-game store.
	/// </summary>
	public interface IStoreService
	{
		/// <summary>
		/// Gets how much a given item is being sold at the store.
		/// If dailyDealStore is not null or empty, we search the price inside this store instead of getting the full price of a given item
		/// </summary>
		Task<FlgStoreItem> GetItemPrice(string itemId, string? dailyDealStore = null);
	}
}