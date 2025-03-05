using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstLightServerSDK.Services
{
	/// <summary>
	/// Represents an item in the in-game item catalog
	/// </summary>
	[Serializable]
	public class FlgCatalogItem
	{
		public string ItemId;
		public string ItemData;
	}
	
	/// <summary>
	/// Handles game item catalog
	/// </summary>
	public interface IItemCatalog<T>
	{
		/// <summary>
		/// Gets a catalog item based on item id
		/// </summary>
		Task<FlgCatalogItem> GetCatalogItemById(string id);

		/// <summary>
		/// Gets game item model by catalog item id
		/// </summary>
		Task<T> GetCatalogItem(string itemId);

		/// <summary>
		/// Gets all catalog items
		/// </summary>
		Task<List<FlgCatalogItem>> GetAllCatalogItems();
	}
}