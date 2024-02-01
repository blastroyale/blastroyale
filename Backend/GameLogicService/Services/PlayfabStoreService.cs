using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLightServerSDK.Services;
using PlayFab;
using PlayFab.ServerModels;

namespace GameLogicService.Services;


public class PlayfabServerStoreService : IStoreService
{
	public const string CATALOG_NAME = "Store";
	public const string STORE_NAME = "MainShop";

	private Dictionary<string, StoreItem> _storeItems;
	private Cooldown _updateCooldown = new (TimeSpan.FromMinutes(1));
	
	public async Task<FlgStoreItem> GetItemPrice(string itemId)
	{
		await FetchStore();
		if (!_storeItems.TryGetValue(itemId, out var storeItem))
		{
			throw new Exception("Could not find store item setup for item id " + storeItem);
		}
		var item = _storeItems[itemId];
		return new FlgStoreItem()
		{
			Price = item.VirtualCurrencyPrices,
			ItemId = item.ItemId
		};
	}
	
	private async Task FetchStore()
	{
		if (_storeItems == null || !_updateCooldown.IsCooldown())
		{
			_updateCooldown.Trigger();
			var result = await PlayFabServerAPI.GetStoreItemsAsync(new GetStoreItemsServerRequest()
			{
				StoreId = STORE_NAME,
				CatalogVersion = CATALOG_NAME
			});
			if (result.Error != null) throw new Exception(result.Error.GenerateErrorReport());
			_storeItems = new();
			foreach (var item in result.Result.Store)
			{
				_storeItems[item.ItemId] = item;
			}
		}
	}

}