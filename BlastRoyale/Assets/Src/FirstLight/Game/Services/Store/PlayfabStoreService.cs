using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLightServerSDK.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Services
{
	public enum StoreDisplaySize
	{
		Full,
		Half
	}

	/// <summary>
	/// Properties that can be configured on Playfab on each store item 
	/// </summary>
	public class StoreItemData
	{
		public StoreDisplaySize Size;
		public int PurchaseCooldown;
		public int MaxAmount;
		public bool ShouldDailyReset;
		public string Category;
		public string UssModifier;
		public string ImageOverride;
		public string Description;
		//Bundle Configurations
		public DateTime PurchaseExpiresAt;
		public bool IsTimeLimitedByPlayer;
		public int TimeLimitedByPlayerExpiresAfterSeconds;
	}

	/// <summary>
	/// Represents an item that was setup in playfab and that can be bought 
	/// </summary>
	public class PlayfabProductConfig
	{
		/// <summary>
		/// Information of the item setup in playfab Item Catalog
		/// </summary>
		public CatalogItem CatalogItem;

		/// <summary>
		/// Specific
		/// </summary>
		public StoreItem StoreItem;

		/// <summary>
		/// Specific Store data to know how to display the item
		/// </summary>
		public StoreItemData StoreItemData;
	}

	/// <summary>
	/// Wraps all playfab specific functionality required for the IAP store to work
	/// Handles the Catalog, Store Items and Prices.
	/// Also handles Custom Playfab Store (Special Deals) loads and Store Setup
	/// </summary>
	public class PlayfabStoreService : IStoreService, IItemCatalog<ItemData>
	{
		private const string CATALOG_NAME = "Store";
		private const string STORE_NAME = "MainShop";
		
		//Daily Deals Special Shops
		private const string LEGENDARY_DAILY_STORE = "LegendaryDailyStore";
		private const string EPIC_DAILY_STORE = "EpicDailyStore";
		private const string RARE_DAILY_STORE = "RareDailyStore";
		private const string COMMON_DAILY_STORE = "CommonDailyStore";
		
		public List<string> SPECIAL_DEALS_STORES = new()
		{
			LEGENDARY_DAILY_STORE, EPIC_DAILY_STORE, RARE_DAILY_STORE, COMMON_DAILY_STORE 
		};

		private IGameBackendService _backendService;
		private IGameCommandService _gameCommandService;
		private IGameDataProvider _dataProvider;

		public event Action<List<PlayfabProductConfig>, Dictionary<PlayfabProductConfig, List<PlayfabProductConfig>>> OnStoreLoaded;
		
		/// <summary>
		/// Catalogs hold information of each item registered inside Economy>Catalog(Legacy)>Catalog
		/// i.e: Item data to make an ItemData instance
		/// </summary>
		private Dictionary<string, CatalogItem> _catalogItems = new ();

		/// <summary>
		/// Store holds store only specific information like which items should be displayed and how to display them
		/// MainStore items are shown for all players
		/// </summary>
		private Dictionary<string, StoreItem> _mainStoreItems = new ();
		
		/// <summary>
		/// Store holds store only specific information like which items should be displayed and how to display them
		/// MainStore items are shown for all players
		/// </summary>
		private Dictionary<string, StoreItem> _dailyDealsStoreItems = new ();
		
		/// <summary>
		/// Hold all information about DailyDeals for Player such as:
		/// - Which time DailyDeals should be reseted and reloaded
		/// - Which Special Stores (Legendary, Epic, Rare, Common) are currently available/active for player
		/// - Chances to show each Special Store (Legendary, Epic, Rare, Common)
		/// - Which Item from Available/Active store are available for this player
		/// This value is persisted inside PlayerStoreData and changes daily, we persist this Configuration because it holds information
		/// that we can manipulate to increase/decrease chance to show certain store for the player as informations like the MaxItems that can be
		/// show for each store
		/// </summary>
		private PlayerDailyDealsConfiguration _playerDailyDealsConfiguration;
		
		/// <summary>
		/// Catalogs hold information of each bundle (Bundles is a set of Items that can be used to make an ItemData instance)
		/// </summary>
		private Dictionary<string, CatalogItem> _bundleItems = new ();
		
		
		/// <summary>
		/// Final result of all playfab data containing all store catalog items, store items, special deals store items
		/// </summary>
		private Dictionary<string, PlayfabProductConfig> _products = new ();
		
		/// <summary>
		/// Final result of all playfab data containing all Bundles in Bundles/BundlesProducts Dictionary
		/// </summary>
		private Dictionary<string, Dictionary<PlayfabProductConfig,List<PlayfabProductConfig>>> _bundles = new ();
		
		/// <summary>
		/// Final result of all playfab data containing all legendary store catalog items and store items
		/// </summary>
		private Dictionary<string, PlayfabProductConfig> _specialStoreProducts = new ();
		
		/// <summary>
		/// Represents a cooldown mechanism for managing the rate of store reloading
		/// </summary>
		private readonly Cooldown _storeLoadCooldown;

		
		public PlayfabStoreService(IGameBackendService backend, IGameCommandService gameCommandService, IGameDataProvider dataProvider)
		{
			_backendService = backend;
			_dataProvider = dataProvider;
			_gameCommandService = gameCommandService;
			_storeLoadCooldown = new Cooldown(TimeSpan.FromMinutes(1));
		}

		
		public void Init()
		{
			TryLoadStore();
		}

		
		public void TryLoadStore()
		{
			if (!_storeLoadCooldown.IsCooldown())
			{
				LoadPlayfabCatalogsAndStoreItems().Forget();
			}
		}

		private async UniTaskVoid LoadPlayfabCatalogsAndStoreItems()
		{
			var catalogItemsTask = AsyncPlayfabAPI.GetCatalogItems(new GetCatalogItemsRequest {CatalogVersion = CATALOG_NAME});
			var mainStoreItemsTask = AsyncPlayfabAPI.GetStoreItems(new GetStoreItemsRequest {CatalogVersion = CATALOG_NAME, StoreId = STORE_NAME});
			var (catalogItemResult, storeItemsResult) = await UniTask.WhenAll(catalogItemsTask, mainStoreItemsTask).Timeout(TimeSpan.FromSeconds(10));
			
			var specialStoresResult = await TryLoadSpecialStores();

			OnCatalogStoreItemsLoadResult(catalogItemResult, storeItemsResult, specialStoresResult);

			_storeLoadCooldown.Trigger();
		}

		private void OnCatalogStoreItemsLoadResult(GetCatalogItemsResult getCatalogItemsResult, GetStoreItemsResult getStoreItemsResult,
												   GetStoreItemsResult[] specialStoresResult)
		{
			if (getCatalogItemsResult.Catalog.Count == 0 || (getStoreItemsResult.Store.Count == 0))
			{
				return;
			}
			

			_catalogItems = getCatalogItemsResult.Catalog.ToDictionary(i => i.ItemId, i => i);
			_mainStoreItems = getStoreItemsResult.Store.ToDictionary(i => i.ItemId, i => i);
			_bundleItems = getCatalogItemsResult.Catalog.Where(c => c.Bundle != null && _mainStoreItems.ContainsKey(c.ItemId)).ToDictionary(i => i.ItemId, i => i);

			ResolveSpecialStores(specialStoresResult);
			
			OnAllStoresLoaded();
		}

		
		private void ResolveSpecialStores(GetStoreItemsResult[] specialStoresResult)
		{
			_playerDailyDealsConfiguration = _dataProvider.PlayerStoreDataProvider.GetPlayerDailyStoreConfiguration();

			if (_playerDailyDealsConfiguration == null || _dataProvider.PlayerStoreDataProvider.IsDailyDealExpired())
			{
				if (_playerDailyDealsConfiguration == null)
				{
					_playerDailyDealsConfiguration = new PlayerDailyDealsConfiguration();	
				}
				
				SetupNewDailyDealsFromStores(specialStoresResult, _playerDailyDealsConfiguration);
				
				PersistPlayerDailyDealsConfiguration(_playerDailyDealsConfiguration);
			}
			else
			{
				FetchPlayerDailyDealsItems(specialStoresResult);
			}
		}
		

		private void FetchPlayerDailyDealsItems(GetStoreItemsResult[] remoteSpecialStoresResult)
		{
			var activeSpecialStores = _playerDailyDealsConfiguration.SpecialStoreList.Where(s => s.IsActive);

			foreach (var playerSpecialStore in activeSpecialStores)
			{
				var remoteSpecialStore = remoteSpecialStoresResult.FirstOrDefault(s => s.StoreId == playerSpecialStore.SpecialStoreName);

				if (remoteSpecialStore != null)
				{
					remoteSpecialStore.Store.Where(rss => playerSpecialStore.SpecialStoreItemIDs.Contains(rss.ItemId)).ToList().ForEach(i =>
					{
						_dailyDealsStoreItems.TryAdd(i.ItemId, i);
					});
				}
			}
		}

		
		private void PersistPlayerDailyDealsConfiguration(PlayerDailyDealsConfiguration playerDailyDealStoreConfiguration)
		{
			_gameCommandService.ExecuteCommand(new UpdatePlayerDailyDealsStoreConfigurationCommand
			{
				PlayerDailyDealsConfiguration = playerDailyDealStoreConfiguration
			});
		}

		
		private void SetupNewDailyDealsFromStores(GetStoreItemsResult[] specialStoresResult, PlayerDailyDealsConfiguration playerDailyDealStoreConfiguration)
		{
			var randomNumberGenerator = new Random(Guid.NewGuid().GetHashCode());
			
			playerDailyDealStoreConfiguration.ResetDealsAt = DateTime.UtcNow.AddDays(1);
			
			var storeLookup = playerDailyDealStoreConfiguration.SpecialStoreList.ToDictionary(s => s.SpecialStoreName);

			foreach (var specialStore in specialStoresResult)
			{
				if (specialStore.MarketingData?.Metadata == null)
					continue; 

				var specialStoreCustomData = JsonConvert.DeserializeObject<FlgSpecialStoreConfiguration>(specialStore.MarketingData.Metadata.ToString());

				if (!storeLookup.TryGetValue(specialStore.StoreId, out var specialStoreConfiguration))
				{
					specialStoreConfiguration = new PlayerSpecialStoreData
					{
						SpecialStoreName = specialStore.StoreId,
						IsActive = false,
						LastAppearance = DateTime.UtcNow,
						SpecialStoreChanceToShow = specialStoreCustomData.StoreBaseChanceToShow
					};
					playerDailyDealStoreConfiguration.SpecialStoreList.Add(specialStoreConfiguration);
				}

				specialStoreConfiguration.IsActive = CalculateSpecialStoreChanceToShow(specialStoreCustomData, specialStoreConfiguration, randomNumberGenerator);

				if (specialStoreConfiguration.IsActive)
				{
					specialStoreConfiguration.LastAppearance = DateTime.UtcNow;
					
					//Select Items for DailyDeals excluding any last Deal for player;
					var selectedItemsForDailyDeals = specialStore.Store
						.Where(i => !specialStoreConfiguration.SpecialStoreItemIDs.Contains(i.ItemId))
						.OrderBy(_ => randomNumberGenerator.Next())
						.Take(specialStoreCustomData.MaxItems).ToList();
					
					specialStoreConfiguration.SpecialStoreItemIDs.Clear(); 
					specialStoreConfiguration.SpecialStoreItemIDs.AddRange(
						selectedItemsForDailyDeals
							.Select(i => i.ItemId)
					);

					foreach (var selectedItem in selectedItemsForDailyDeals)
					{
						_dailyDealsStoreItems.TryAdd(selectedItem.ItemId, selectedItem);
					}
					
				}
			}
		}

		private bool CalculateSpecialStoreChanceToShow(FlgSpecialStoreConfiguration specialStoreCustomData, PlayerSpecialStoreData currentSpecialStore, Random randomNumberGenerator)
		{
			var randomRoll = randomNumberGenerator.NextDouble() * 100;
			
			return randomRoll < Math.Clamp(currentSpecialStore.SpecialStoreChanceToShow, 
										   specialStoreCustomData.StoreBaseChanceToShow, 
										   specialStoreCustomData.StoreMaxChanceToShow);
		}
		

		private UniTask<GetStoreItemsResult[]> TryLoadSpecialStores()
		{
			var fetchSpecialStoreTaskList = new List<UniTask<GetStoreItemsResult>>();
			
			var playerDailyDealStoreConfiguration = _dataProvider.PlayerStoreDataProvider.GetPlayerDailyStoreConfiguration();
			
			if (playerDailyDealStoreConfiguration == null || _dataProvider.PlayerStoreDataProvider.IsDailyDealExpired())
			{
				foreach (var specialStoreName in SPECIAL_DEALS_STORES)
				{
					fetchSpecialStoreTaskList.Add(AsyncPlayfabAPI.GetStoreItems(new GetStoreItemsRequest {CatalogVersion = CATALOG_NAME, StoreId = specialStoreName}));
				}
			}
			else
			{
				var playerCurrentSpecialStores = playerDailyDealStoreConfiguration.SpecialStoreList.Select(s => s.SpecialStoreName).ToList();

				foreach (var specialStoreName in playerCurrentSpecialStores)
				{
					fetchSpecialStoreTaskList.Add(AsyncPlayfabAPI.GetStoreItems(new GetStoreItemsRequest {CatalogVersion = CATALOG_NAME, StoreId = specialStoreName}));
				}
			}

			return UniTask.WhenAll(fetchSpecialStoreTaskList).Timeout(TimeSpan.FromSeconds(10));
		}

		
		private void OnAllStoresLoaded()
		{
			_products.Clear();
			
			foreach (var (itemId, storeItem) in _dailyDealsStoreItems)
			{
				_products.TryAdd(itemId, new PlayfabProductConfig()
				{
					CatalogItem = _catalogItems[itemId],
					StoreItem = storeItem,
					StoreItemData = ModelSerializer.Deserialize<StoreItemData>(storeItem.CustomData.ToString())
				});
			}

			foreach (var (itemId, storeItem) in _mainStoreItems)
			{
				_products.TryAdd(itemId, new PlayfabProductConfig()
				{
					CatalogItem = _catalogItems[itemId],
					StoreItem = storeItem,
					StoreItemData = ModelSerializer.Deserialize<StoreItemData>(storeItem.CustomData.ToString())
				});
			}
			
			foreach (var (itemId, bundleCatalogItem) in _bundleItems)
			{
				var bundleProducts = _catalogItems
					.Where(item => bundleCatalogItem.Bundle.BundledItems.Contains(item.Key))
					.Select(item => new PlayfabProductConfig()
					{
						CatalogItem = item.Value
					})
					.ToList();

				var bundleStoreItem = _mainStoreItems?.FirstOrDefault(s => s.Value.ItemId == bundleCatalogItem.ItemId).Value;
				
				_bundles[itemId] = new Dictionary<PlayfabProductConfig, List<PlayfabProductConfig>>
				{
					{
						new PlayfabProductConfig()
						{
							CatalogItem = bundleCatalogItem,
							StoreItem = bundleStoreItem,
							StoreItemData = bundleStoreItem != null ? ModelSerializer.Deserialize<StoreItemData>(bundleStoreItem.CustomData.ToString()) : null
						},
						bundleProducts
					}
				};
			}

			var bundleProductsDictionary = _bundles.SelectMany(entry => entry.Value)
												   .ToDictionary(pair => pair.Key, pair => pair.Value);

			OnStoreLoaded?.Invoke(_products.Values.ToList(), bundleProductsDictionary);
		}
		

		private void HandlePlayfabRequestError(PlayFabError error)
		{
			_backendService.HandleError(error, null);
		}
		

		public void AskBackendForItem(Product product, Action<Product, ItemData> onRewarded)
		{
			FLog.Info($"Purchase validated: {product.definition.id}");
			var data = new Dictionary<string, string>
			{
				{"item_id", product.definition.id},
			};

			_backendService.CallGenericFunction(CommandNames.CONSUME_VALIDATE_PURCHASE, result =>
			{
				FLog.Info($"Purchase handled by the server: {product.definition.id}, {result.FunctionName}");
				var logicResult =
					JsonConvert.DeserializeObject<PlayFabResult<LogicResult>>(result.FunctionResult.ToString());
				var item = ModelSerializer.DeserializeFromData<ItemData>(logicResult!.Result.Data);
				onRewarded.Invoke(product, item);
			}, HandlePlayfabRequestError, data);
		}

		
		public void ValidateReceipt(Product product, Action<Product> onValidated)
		{
			var cacheProduct = product;
			var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(product.receipt)!["Payload"]
				.ToString();
			var currencyCode = product.metadata.isoCurrencyCode;
			var price = product.metadata.localizedPrice * 100;

#if UNITY_IOS
			var request = new ValidateIOSReceiptRequest
			{
				CurrencyCode = currencyCode,
				PurchasePrice = (int) price,
				ReceiptData = payload
			};
			PlayFabClientAPI.ValidateIOSReceipt(request, _ => onValidated(cacheProduct), HandlePlayfabRequestError);
#else
			
			var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
			var request = new ValidateGooglePlayPurchaseRequest
			{
				CurrencyCode = currencyCode,
				PurchasePrice = (uint) price,
				ReceiptJson = (string) data["json"],
				Signature = (string) data["signature"]
			};
			PlayFabClientAPI.ValidateGooglePlayPurchase(request, r => onValidated(cacheProduct), HandlePlayfabRequestError);
#endif
		}

		public Task<FlgStoreItem> GetItemPrice(string itemId, string dailyDealStore = null)
		{

			var i = dailyDealStore != null ? _dailyDealsStoreItems[itemId] : _mainStoreItems[itemId];
			
			return Task.FromResult(new FlgStoreItem()
			{
				ItemId = i.ItemId,
				Price = i.VirtualCurrencyPrices
			});
		}

		public Task<ItemData> GetItemData(string itemId) => GetCatalogItem(itemId);

		public Task<FlgCatalogItem> GetCatalogItemById(string itemId)
		{
			var i = _catalogItems[itemId];
			return Task.FromResult(new FlgCatalogItem()
			{
				ItemId = i.ItemId,
				ItemData = i.CustomData
			});
		}

		public Task<ItemData> GetCatalogItem(string itemId)
		{
			return Task.FromResult(ItemFactory.PlayfabCatalog(_catalogItems[itemId]));
		}

		public Task<List<FlgCatalogItem>> GetAllCatalogItems()
		{
			return Task.FromResult(_catalogItems.Select(e => new FlgCatalogItem()
			{
				ItemId = e.Value.ItemId,
				ItemData = e.Value.CustomData
			}).ToList());
		}
	}
}