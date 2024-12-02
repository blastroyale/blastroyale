using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace FirstLight.Game.Services
{
	public interface IUnityStoreService
	{
		public class PurchaseFailureData
		{
			public string ProductId;
			public PurchaseFailureReason Reason;
			public string Message;
		}

		/// <summary>
		/// Event for when a store purchase goes wrong
		/// </summary>
		public event Action<PurchaseFailureData> OnPurchaseFailure;

		/// <summary>
		/// Request if the IAP service has been properly initialized.
		/// </summary>
		bool Initialized { get; }

		/// <summary>
		/// Holds all operational logic in Unity IAP SDK to purchase items
		/// </summary>
		IStoreController Controller { get; }
	}

	/// <summary>
	/// Unity Store API listener, specific to hook into Unity's SDK
	/// </summary>
	public class UnityStoreService : IDetailedStoreListener, IUnityStoreService
	{
		public event Action<IUnityStoreService.PurchaseFailureData> OnPurchaseFailure;

		public IStoreController Controller { get; private set; }
		public IExtensionProvider Extensions { get; private set; }
		public bool Initialized { get; private set; }

		public Product GetUnityProduct(string id)
		{
			return _products[id];
		}

		private Dictionary<string, Product> _products;

		private Func<PurchaseEventArgs, PurchaseProcessingResult> _onPurchaseHandler;

		public UnityStoreService(Func<PurchaseEventArgs, PurchaseProcessingResult> purchaseHandler)
		{
			_onPurchaseHandler = purchaseHandler;
			_products = new Dictionary<string, Product>();
		}

		public void InitializeUnityCatalog(HashSet<string> catalogItemIds)
		{
			var module = GetPurchasingModule();
			var builder = ConfigurationBuilder.Instance(module);
			foreach (var itemId in catalogItemIds)
			{
				builder.AddProduct(itemId, ProductType.Consumable);
			}

			UnityPurchasing.Initialize(this, builder);
		}

		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			Extensions = extensions;
			Controller = controller;
			foreach (var p in controller.products.all)
			{
				_products[p.definition.id] = p;
			}

			FLog.Info("IAP", controller.GetType().Name);
			FLog.Info("IAP", $"Initialized Unity SDK with Products {JsonConvert.SerializeObject(controller.products.all)}");
			Initialized = true;
		}

		private StandardPurchasingModule GetPurchasingModule()
		{
#if UNITY_ANDROID
			var module = StandardPurchasingModule.Instance(AppStore.GooglePlay);
#elif UNITY_IOS
			var module = StandardPurchasingModule.Instance(AppStore.AppleAppStore);
#else
			var module = StandardPurchasingModule.Instance();
#endif

#if DEVELOPMENT_BUILD
			var useFakeStore = PlayerPrefs.GetInt("Debug.UseFakeStore", 1) == 1;
			var fakeStoreUiMode =
				Enum.Parse<FakeStoreUIMode>(PlayerPrefs.GetString("Debug.FakeStoreUiMode",
					FakeStoreUIMode.Default.ToString()));
			module.useFakeStoreAlways = useFakeStore;
			module.useFakeStoreUIMode = fakeStoreUiMode;
#endif
			FLog.Info("IAP", "Module: " + module.GetType().Name);
			return module;
		}

		public void OnInitializeFailed(InitializationFailureReason error)
		{
			OnInitializeFailed(error, null);
		}

		public void OnInitializeFailed(InitializationFailureReason error, string message)
		{
			FLog.Error("IAP", $"Unity Store initialization failed: {error} - {message}");
			Initialized = false;
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
		{
			return _onPurchaseHandler.Invoke(purchaseEvent);
		}

		public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
		{
			var reason = failureDescription.reason;
			FLog.Error("IAP", $"Purchase failed: {product.definition.id}, {failureDescription.reason} {failureDescription.message}");
			// Google/Unity SDK returns unknown for this 
			if (failureDescription.message.Contains("There is already a pending purchase"))
			{
				reason = PurchaseFailureReason.ExistingPurchasePending;
			}

			OnPurchaseFailure?.Invoke(new IUnityStoreService.PurchaseFailureData()
			{
				ProductId = product.definition.id,
				Reason = reason,
				Message = failureDescription.message
			});
		}

		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			FLog.Error("IAP", $"Purchase failed: {product.definition.id}, {failureReason}");
			OnPurchaseFailure?.Invoke(new IUnityStoreService.PurchaseFailureData()
			{
				ProductId = product.definition.id,
				Reason = failureReason,
				Message = null
			});
		}
	}
}