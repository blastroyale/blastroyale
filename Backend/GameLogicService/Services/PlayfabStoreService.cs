using System;
using System.Collections.Generic;
using System.Linq;
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
	private int _currentStoreHashcode;
	
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
		if (!_updateCooldown.IsCooldown())
		{
			_updateCooldown.Trigger();

			await UpdateStoreState();
		}
	}

	private async Task UpdateStoreState()
	{
		var result = await PlayFabServerAPI.GetStoreItemsAsync(new GetStoreItemsServerRequest
		{
			StoreId = STORE_NAME,
			CatalogVersion = CATALOG_NAME
		});
		
		if (result.Error != null) throw new Exception(result.Error.GenerateErrorReport());

		if (!HasStoreHashcodeChanged(result.Result.Store))
		{
			_storeItems = new Dictionary<string, StoreItem>();
			
			foreach (var item in result.Result.Store)
			{
				_storeItems[item.ItemId] = item;
			}
		}
	}

	private bool HasStoreHashcodeChanged(List<StoreItem> storeItems)
	{
		var newHash = CalculateStoreHashcode(storeItems);

		if (_currentStoreHashcode == newHash)
		{
			return true;
		}

		_currentStoreHashcode = newHash;
		return false;

	}

	private static int CalculateStoreHashcode(List<StoreItem> storeItems)
	{
		if (storeItems.Count == 0)
		{
			return 0;
		}

		unchecked
		{
			var hash = 17;
		
			foreach (var s in storeItems.OrderBy(entry => entry.ItemId))
			{
				hash = hash * 23 + (s.ItemId != null ? s.ItemId.GetHashCode() : 0);
				
				if (s.RealCurrencyPrices != null)
				{
					foreach (var kvp in s.RealCurrencyPrices)
					{
						hash = hash * 23 + kvp.Key.GetHashCode();
						hash = hash * 23 + kvp.Value.GetHashCode();
					}
				}
				
				if (s.VirtualCurrencyPrices != null)
				{
					foreach (var kvp in s.VirtualCurrencyPrices)
					{
						hash = hash * 23 + kvp.Key.GetHashCode();
						hash = hash * 23 + kvp.Value.GetHashCode();
					}
				}
			}

			return hash;
		}
	}

}