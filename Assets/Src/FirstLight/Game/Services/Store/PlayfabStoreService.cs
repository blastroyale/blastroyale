using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Server.SDK.Modules;
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
		public string Category;
		public string UssModifier;
		public string ImageOverride;
		public string Description;
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
		public event Action<List<PlayfabProductConfig>> OnStoreLoaded;
		
		public const string CATALOG_NAME = "Store";
		public const string STORE_NAME = "MainShop";
		
		private IGameBackendService _backend;
		private IGameCommandService _commands;

		/// <summary>
		/// Catalogs hold information of each item (like item data to make an ItemData instance)
		/// </summary>
		private Dictionary<string, CatalogItem> _catalogItems = new();
		
		/// <summary>
		/// Store holds store only specific information like which items should be displayed and how to display them
		/// </summary>
		private Dictionary<string, StoreItem> _storeItems = new ();

		/// <summary>
		/// Final result of all playfab data containing all store catalog items and store items
		/// </summary>
		private Dictionary<string, PlayfabProductConfig> _products = new ();

		public PlayfabStoreService(IGameBackendService backend, IGameCommandService commands)
		{
			_backend = backend;
			_commands = commands;
		}
		
		public void Init()
		{
			PlayFabClientAPI.GetCatalogItems(new () {CatalogVersion = CATALOG_NAME }, OnCatalogResult, OnError);
			PlayFabClientAPI.GetStoreItems(new () {CatalogVersion = CATALOG_NAME, StoreId = STORE_NAME}, OnShopResult, OnError);
		}
		
		private void OnShopResult(GetStoreItemsResult res)
		{
			_storeItems = res.Store.ToDictionary(i => i.ItemId, i => i);
			if (_catalogItems.Count > 0) OnLoaded();
		}
		
		private void OnCatalogResult(GetCatalogItemsResult result)
		{
			_catalogItems = result.Catalog.ToDictionary(i => i.ItemId, i => i);
			if(_storeItems.Count > 0) OnLoaded();
		}

		private void OnLoaded()
		{
			foreach (var (itemId, storeItem) in _storeItems)
			{
				_products[itemId] = new PlayfabProductConfig()
				{
					CatalogItem = _catalogItems[itemId],
					StoreItem = storeItem,
					StoreItemData = ModelSerializer.Deserialize<StoreItemData>(storeItem.CustomData.ToString())
				};
			}
			OnStoreLoaded?.Invoke(_products.Values.ToList());
		}

		private void OnError(PlayFabError error)
		{
			_backend.HandleError(error, null, AnalyticsCallsErrors.ErrorType.Recoverable);
		}

		public void AskBackendForItem(Product product, Action<Product, ItemData> onRewarded)
		{
			FLog.Info($"Purchase validated: {product.definition.id}");
			var request = new LogicRequest
			{
				Command = "ConsumeValidatedPurchaseCommand",
				Platform = Application.platform.ToString(),
				Data = new Dictionary<string, string>
				{
					{ "item_id", product.definition.id },
				}
			};

			_backend.CallFunction(request.Command, result =>
			{
				FLog.Info($"Purchase handled by the server: {product.definition.id}, {result.FunctionName}");
				var logicResult =
					JsonConvert.DeserializeObject<PlayFabResult<LogicResult>>(result.FunctionResult.ToString());
				var item = ModelSerializer.DeserializeFromData<ItemData>(logicResult!.Result.Data);
				onRewarded.Invoke(product, item);
			}, OnError, request);
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
			PlayFabClientAPI.ValidateIOSReceipt(request, _ => onValidated(cacheProduct), OnError);
#else
			var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
			var request = new ValidateGooglePlayPurchaseRequest
			{
				CurrencyCode = currencyCode,
				PurchasePrice = (uint) price,
				ReceiptJson = (string) data["json"],
				Signature = (string) data["signature"]
			};
			PlayFabClientAPI.ValidateGooglePlayPurchase(request, r => onValidated(cacheProduct), OnError);
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