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
	
	/// <summary>
	/// Represents server setup of the in-game store.
	/// </summary>
	public interface IStoreService
	{
		/// <summary>
		/// Gets how much a given item is being sold at the store.
		/// </summary>
		Task<FlgStoreItem> GetItemPrice(string itemId);
	}
}