using System;
using System.Collections.Generic;
using AppsFlyerSDK;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Services
{
	public struct ProductData
	{
		/// <summary>
		/// Requests the product Id
		/// </summary>
		public string Id { get; }
		
		/// <summary>
		/// Requests the item reward content of this purchase
		/// </summary>
		public CatalogItemCustomData Data { get; }
		
		/// <summary>
		/// Requests this product metadata
		/// </summary>
		public ProductMetadata Metadata { get; internal set; }
		
		/// <summary>
		/// Requests the product localized price
		/// </summary>
		public string Price => Data.PriceGameId == GameId.RealMoney ? Metadata.localizedPriceString : Data.PriceValue.ToString();

		public ProductData(string id, CatalogItemCustomData data)
		{
			Id = id;
			Data = data;
			Metadata = new ProductMetadata(Data.PriceValue.ToString(), $"Shop/{id}", $"Shop/{id}Description", 
			                               Data.PriceGameId.ToString(), (decimal) Data.PriceValue);
		}
	}
	
	/// <summary>
	/// This service
	/// </summary>
	public interface IStoreService
	{
		/// <summary>
		/// Requests the list present the game's store products
		/// </summary>
		IReadOnlyList<ProductData> Products { get; }
		
		/// <summary>
		/// Initializes the service with the given IAP <paramref name="items"/>
		/// </summary>
		void Init(List<CatalogItem> items);

		/// <summary>
		/// Buys the product defined by the given <paramref name="id"/>
		/// </summary>
		void BuyProduct(string id);
	}
	
	/// <inheritdoc cref="IStoreService" />
	public class StoreService : IStoreService, IStoreListener
	{
		public static readonly string StoreCatalogVersion = "Store";
		
		private readonly IGameCommandService _commandService;
		private readonly List<ProductData> _products = new List<ProductData>();
		
		private IStoreController _store;

		/// <inheritdoc />
		public IReadOnlyList<ProductData> Products => _products;

		public StoreService(IGameCommandService commandService)
		{
			_commandService = commandService;
		}

		/// <inheritdoc />
		public void Init(List<CatalogItem> items)
		{
			var module = StandardPurchasingModule.Instance();

			if (Debug.isDebugBuild)
			{
				module.useFakeStoreAlways = true;
				module.useFakeStoreUIMode = FakeStoreUIMode.Default;
			}
			
			var builder = ConfigurationBuilder.Instance(module);

			foreach (var item in items)
			{
				var customData = JsonConvert.DeserializeObject<CatalogItemCustomData>(item.CustomData);
				var product = new ProductData(item.ItemId, customData);
				
				_products.Add(product);

				if (product.Data.PriceGameId != GameId.RealMoney)
				{
					continue;
				}
				
				var ids = new IDs {{item.ItemId, GooglePlay.Name}, {item.ItemId, AppleAppStore.Name}}; 
				var payoutDefinition = new PayoutDefinition(PayoutType.Currency, product.Data.RewardGameId.ToString(),
				                                            product.Data.RewardValue, item.CustomData);
					
				builder.AddProduct(item.ItemId, ProductType.Consumable, ids, payoutDefinition);
			}
			
			UnityPurchasing.Initialize(this, builder);
		}

		/// <inheritdoc />
		public void BuyProduct(string id)
		{
			if (_store == null)
			{
				throw new InvalidOperationException("The IAP store was not initialized yet");
			}

			var product = _products.Find(data => data.Id == id);
			
			if (product.Data.PriceGameId != GameId.RealMoney)
			{
				_commandService.ExecuteCommand(new ConsumeIapCommand
				{
					Product = product,
					IsNotIap = true,
					FailureReason = null
				});
				return;
			}
			
			_store.InitiatePurchase(id);
		}

		/// <inheritdoc />
		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			_store = controller;

			foreach (var product in controller.products.all)
			{
				for (var i = 0; i < _products.Count; i++)
				{
					if (_products[i].Id == product.definition.id)
					{
						var productData = _products[i];
						var metadata = new ProductMetadata(product.metadata.localizedPriceString,
						                                   productData.Metadata.localizedTitle,
						                                   productData.Metadata.localizedDescription,
						                                   product.metadata.isoCurrencyCode,
						                                   product.metadata.localizedPrice);

						productData.Metadata = metadata;
						_products[i] = productData;
						
						break;
					}
				}
			}
			
			PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnValidatedNotConsumedPurchases, GameCommandService.OnPlayFabError);
			AppsFlyer.setCurrencyCode(_store.products.all[0].metadata.isoCurrencyCode);
		}

		/// <inheritdoc />
		public void OnInitializeFailed(InitializationFailureReason error)
		{
			throw new UnityException($"Unable to initialize {Application.platform} IAP store - {error}");
		}

		/// <inheritdoc />
		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
		{
			var cacheProduct = purchaseEvent.purchasedProduct;
			
			PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnRequestSuccess, OnRequestFailed);
			
			return PurchaseProcessingResult.Pending;
			
			void OnRequestSuccess(GetUserInventoryResult result)
			{
				if (WasPurchased(result.Inventory, cacheProduct.definition.id) || IsFakeStore(cacheProduct))
				{
					PurchaseValidated(cacheProduct);
					return;
				}

				ValidateReceipt(cacheProduct);
			}

			void OnRequestFailed(PlayFabError error)
			{
				OnPurchaseFailed(cacheProduct, PurchaseFailureReason.Unknown);
				GameCommandService.OnPlayFabError(error);
			}
		}

		/// <inheritdoc />
		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			_commandService.ExecuteCommand(new ConsumeIapCommand
			{
				Product = _products.Find(data => data.Id == product.definition.id),
				IsNotIap = IsFakeStore(product),
				FailureReason = failureReason
			});
		}

		private void OnValidatedNotConsumedPurchases(GetUserInventoryResult result)
		{
			foreach (var product in Products)
			{
				if (!WasPurchased(result.Inventory, product.Id))
				{
					continue;
				}
				
				_commandService.ExecuteCommand(new ConsumeIapCommand
				{
					Product = product,
					IsNotIap = false,
					FailureReason = null
				});
			}
		}

		private void PurchaseValidated(Product product)
		{
			_store.ConfirmPendingPurchase(product);
			_commandService.ExecuteCommand(new ConsumeIapCommand
			{
				Product = _products.Find(data => data.Id == product.definition.id),
				IsNotIap = IsFakeStore(product),
				FailureReason = null
			});
		}

		private bool WasPurchased(List<ItemInstance> items, string productId)
		{
			foreach (var item in items)
			{
				if (item.ItemId == productId)
				{
					return true;
				}
			}
			
			return false;
		}

		private bool IsFakeStore(Product product)
		{
			return !product.hasReceipt || string.IsNullOrEmpty(product.receipt) || product.receipt.Contains("\"Store\":\"fake\"");
		}

		private void ValidateReceipt(Product product)
		{
			var cacheProduct = product;
			var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(product.receipt)["Payload"].ToString();
			var currencyCode = product.metadata.isoCurrencyCode;
			var price = product.metadata.localizedPrice * 100;
			
#if UNITY_IOS
			var request = new ValidateIOSReceiptRequest
			{
				CurrencyCode = currencyCode,
				PurchasePrice = (int) price,
				ReceiptData = payload
			};
			
			PlayFabClientAPI.ValidateIOSReceipt(request, result => PurchaseValidated(cacheProduct), GameCommandService.OnPlayFabError);
#else
			
			var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
			var request = new ValidateGooglePlayPurchaseRequest
			{
				CurrencyCode = currencyCode,
				PurchasePrice = (uint) price,
				ReceiptJson = (string) data["json"],
				Signature = (string) data["signature"]
			};
			
			PlayFabClientAPI.ValidateGooglePlayPurchase(request, result => PurchaseValidated(cacheProduct), GameCommandService.OnPlayFabError);
#endif
		}
	}
}