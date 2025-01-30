using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLightServerSDK.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;
using UnityEngine;
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
	/// Handles the Catalog, Store Items and Prices
	/// </summary>
	public class PlayfabStoreService : IStoreService, IItemCatalog<ItemData>
	{
		public event Action<List<PlayfabProductConfig>, Dictionary<PlayfabProductConfig, List<PlayfabProductConfig>>> OnStoreLoaded;

		public const string CATALOG_NAME = "Store";
		// public const string STORE_NAME = "Marcelo";
		public const string STORE_NAME = "MainShop";

		private IGameBackendService _backend;

		/// <summary>
		/// Catalogs hold information of each item (like item data to make an ItemData instance)
		/// </summary>
		private Dictionary<string, CatalogItem> _catalogItems = new ();

		/// <summary>
		/// Store holds store only specific information like which items should be displayed and how to display them
		/// </summary>
		private Dictionary<string, StoreItem> _storeItems = new ();

		/// <summary>
		/// Catalogs hold information of each bundle (Bundles is a set of Items that can be used to make an ItemData instance)
		/// </summary>
		private Dictionary<string, CatalogItem> _bundleItems = new ();
		
		/// <summary>
		/// Final result of all playfab data containing all store catalog items and store items
		/// </summary>
		private Dictionary<string, PlayfabProductConfig> _products = new ();
		
		/// <summary>
		/// Final result of all playfab data containing all Bundles in Bundles/BundlesProducts Dictionary
		/// </summary>
		private Dictionary<string, Dictionary<PlayfabProductConfig,List<PlayfabProductConfig>>> _bundles = new ();
		
		

		/// <summary>
		/// Represents a cooldown mechanism for managing the rate of store reloading
		/// </summary>
		private readonly Cooldown _storeLoadCooldown;

		public PlayfabStoreService(IGameBackendService backend)
		{
			_backend = backend;
			_storeLoadCooldown = new Cooldown(TimeSpan.FromMinutes(30));
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
			var storeItemsTask = AsyncPlayfabAPI.GetStoreItems(new GetStoreItemsRequest {CatalogVersion = CATALOG_NAME, StoreId = STORE_NAME});

			var (catalogItemResult, storeItemsResult) = await UniTask.WhenAll(catalogItemsTask, storeItemsTask).Timeout(TimeSpan.FromSeconds(10));

			OnCatalogStoreItemsLoadResult(catalogItemResult, storeItemsResult);

			_storeLoadCooldown.Trigger();
		}

		private void OnCatalogStoreItemsLoadResult(GetCatalogItemsResult getCatalogItemsResult, GetStoreItemsResult getStoreItemsResult)
		{
			if (getCatalogItemsResult.Catalog.Count == 0 || getStoreItemsResult.Store.Count == 0)
			{
				return;
			}

			_catalogItems = getCatalogItemsResult.Catalog.ToDictionary(i => i.ItemId, i => i);
			_storeItems = getStoreItemsResult.Store.ToDictionary(i => i.ItemId, i => i);
			_bundleItems = getCatalogItemsResult.Catalog.Where(c => c.Bundle != null).ToDictionary(i => i.ItemId, i => i);

			OnLoaded();
		}

		private void OnLoaded()
		{
			_products.Clear();

			foreach (var (itemId, storeItem) in _storeItems)
			{
				_products[itemId] = new PlayfabProductConfig()
				{
					CatalogItem = _catalogItems[itemId],
					StoreItem = storeItem,
					StoreItemData = ModelSerializer.Deserialize<StoreItemData>(storeItem.CustomData.ToString())
				};
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

				var bundleStoreItem = _storeItems?.FirstOrDefault(s => s.Value.ItemId == bundleCatalogItem.ItemId).Value;
				
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
			_backend.HandleError(error, null);
		}

		public void AskBackendForItem(Product product, Action<Product, ItemData> onRewarded)
		{
			FLog.Info($"Purchase validated: {product.definition.id}");
			var data = new Dictionary<string, string>
			{
				{"item_id", product.definition.id},
			};

			_backend.CallGenericFunction(CommandNames.CONSUME_VALIDATE_PURCHASE, result =>
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

		public Task<FlgStoreItem> GetItemPrice(string itemId)
		{
			var i = _storeItems[itemId];
			return Task.FromResult(new FlgStoreItem()
			{
				ItemId = i.ItemId,
				Price = i.VirtualCurrencyPrices
			});
		}

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
	}
}