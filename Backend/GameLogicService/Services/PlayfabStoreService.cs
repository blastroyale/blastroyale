using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using FirstLightServerSDK.Services;
using PlayFab;
using PlayFab.ServerModels;

namespace GameLogicService.Services;


public class PlayfabServerStoreService : IStoreService
{
	private const string CATALOG_NAME = "Store";

	private const string MAIN_STORE_NAME = "MainShop";
	private const string LEGENDARY_DAILY_STORE = "LegendaryDailyStore";
	private const string EPIC_DAILY_STORE = "EpicDailyStore";
	private const string RARE_DAILY_STORE = "RareDailyStore";
	private const string COMMON_DAILY_STORE = "CommonDailyStore";
	
	public List<string> STORE_NAMES = new()
	{
		MAIN_STORE_NAME, LEGENDARY_DAILY_STORE, EPIC_DAILY_STORE, RARE_DAILY_STORE, COMMON_DAILY_STORE 
	};

	private IItemCatalog<ItemData> _catalog;
	private List<PlayfabStoreConfiguration> _loadedStores = new();
	
	private Cooldown _updateCooldown = new (TimeSpan.FromMinutes(5));
	
	public PlayfabServerStoreService(IItemCatalog<ItemData> catalog)
	{
		_catalog = catalog;
	}
	
	public async Task<FlgStoreItem> GetItemPrice(string itemId, string? dailyDealStore = null)
	{
		await FetchStore();

		PlayfabStoreConfiguration storeConfiguration;
		
		if (!string.IsNullOrEmpty(dailyDealStore))
		{
			storeConfiguration = _loadedStores.First(sc => sc.StoreName.Equals(dailyDealStore));
		} 
		else 
		{
			storeConfiguration = _loadedStores.First(sc => sc.StoreName.Equals(MAIN_STORE_NAME));
		}

		if (storeConfiguration == null)
		{
			throw new Exception("Could not find StoreConfiguration for Store: " + 
				(string.IsNullOrEmpty(dailyDealStore) ? MAIN_STORE_NAME : dailyDealStore));
		}

		if (!storeConfiguration.StoreItems.TryGetValue(itemId, out var storeItem))
		{
			throw new Exception(
				$"Could not find store item setup for item id {storeItem} inside {storeConfiguration.StoreName}");
		}
		
		return new FlgStoreItem()
		{
			Price = storeItem.VirtualCurrencyPrices,
			ItemId = storeItem.ItemId
		};
	}

	public Task<ItemData> GetItemData(string itemId)
	{
		return _catalog.GetCatalogItem(itemId);
	}

	private async Task FetchStore()
	{
		if (!_updateCooldown.IsCooldown())
		{
			_updateCooldown.Trigger();

			await UpdateAllStoreState();
		}
	}

	private async Task UpdateAllStoreState()
	{
		foreach (var storeName in STORE_NAMES)
		{
			var fetchedItems = await FetchItemsFromPlayfabStore(storeName);

			var _previouslyLoadedStore = _loadedStores.FirstOrDefault(s => s.StoreName.Equals(storeName));

			if (_previouslyLoadedStore != null)
			{
				_previouslyLoadedStore.UpdateStoreItems(fetchedItems);
				return;
			}
			
			_loadedStores.Add(new PlayfabStoreConfiguration(storeName, fetchedItems));
		}		
	}

	private async Task<List<StoreItem>> FetchItemsFromPlayfabStore(string storeName)
	{
		var result = await PlayFabServerAPI.GetStoreItemsAsync(new GetStoreItemsServerRequest
		{
			StoreId = storeName,
			CatalogVersion = CATALOG_NAME
		});
	
		if (result.Error != null) throw new Exception(result.Error.GenerateErrorReport());

		return result.Result.Store;
	}
}

public class PlayfabStoreConfiguration
{

	public string StoreName;

	public Dictionary<string, StoreItem> StoreItems;

	private int CurrentStoreHashcode { get; set; }
	
	
	public PlayfabStoreConfiguration(string storeName, List<StoreItem> storeItems)
	{
		StoreName = storeName;
		UpdateStoreItems(storeItems);
	}

	public void UpdateStoreItems(List<StoreItem> storeItemsResult)
	{
		if (!HasStoreHashcodeChanged(storeItemsResult))
		{
			StoreItems = new Dictionary<string, StoreItem>();
			
			foreach (var item in storeItemsResult)
			{
				StoreItems[item.ItemId] = item;
			}
		}
	}


	private bool HasStoreHashcodeChanged(List<StoreItem> storeItems)
	{
		var newHash = CalculateStoreHashcode(storeItems);

		if (CurrentStoreHashcode == newHash)
		{
			return true;
		}

		CurrentStoreHashcode = newHash;
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