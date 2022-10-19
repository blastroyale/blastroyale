using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Commands.OfflineCommands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;
using UnityEngine;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service handles all the IAP logic for our in-game store.
	/// </summary>
	public interface IIAPService
	{
		/// <summary>
		/// Request if the IAP service has been properly initialized.
		/// </summary>
		IObservableFieldReader<bool> Initialized { get; }

		/// <summary>
		/// Requests the list present the game's store products
		/// </summary>
		IReadOnlyList<Product> Products { get; }

		/// <summary>
		/// Buys the product defined by the given <paramref name="id"/>
		/// </summary>
		void BuyProduct(string id);
	}

	public class IAPService : IIAPService, IStoreListener
	{
		public IObservableFieldReader<bool> Initialized => _initialized;
		public IReadOnlyList<Product> Products => _products;

		private List<Product> _products;
		private readonly IObservableField<bool> _initialized;

		private readonly IGameCommandService _commandService;
		private readonly IMessageBrokerService _messageBroker;
		private readonly IPlayfabService _playfabService;

		private IStoreController _store;

		public IAPService(IGameCommandService commandService, IMessageBrokerService messageBroker,
						  IPlayfabService playfabService)
		{
			_commandService = commandService;
			_messageBroker = messageBroker;
			_playfabService = playfabService;

			_products = new List<Product>();
			_initialized = new ObservableField<bool>(false);

			var module = StandardPurchasingModule.Instance();

			if (Debug.isDebugBuild)
			{
				module.useFakeStoreAlways = true;
				module.useFakeStoreUIMode = FakeStoreUIMode.Default;
			}

			var builder = ConfigurationBuilder.Instance(module);

			IAPConfigurationHelper.PopulateConfigurationBuilder(ref builder, ProductCatalog.LoadDefaultCatalog());

			UnityPurchasing.Initialize(this, builder);
		}

		public void BuyProduct(string id)
		{
			FLog.Info($"Purchase initiated: {id}");
			_store.InitiatePurchase(id);
		}

		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			_store = controller;
			_products = controller.products.all.ToList();

			_initialized.Value = true;
			FLog.Info("IAP Initialized");
		}

		public void OnInitializeFailed(InitializationFailureReason error)
		{
			FLog.Warn($"IAP Initialization failed: {error}");

			_initialized.Value = false;
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
		{
			var fakeStore = IsFakeStore(purchaseEvent.purchasedProduct);
			FLog.Info($"Purchase processed: {purchaseEvent.purchasedProduct.definition.id}, Fake store: {fakeStore}");
			
			if (fakeStore)
			{
				PurchaseValidated(purchaseEvent.purchasedProduct);
			}
			else
			{
				ValidateReceipt(purchaseEvent.purchasedProduct);
			}

			return PurchaseProcessingResult.Pending;
		}

		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			FLog.Warn($"Purchase failed: {product.definition.id}, {failureReason}");
			_messageBroker.Publish(new IAPPurchaseFailedMessage {Reason = failureReason});
		}

		private void PurchaseValidated(Product product)
		{
			FLog.Info($"Purchase validated: {product.definition.id}");

			var request = new LogicRequest
			{
				Command = "ConsumeValidatedPurchaseCommand",
				Platform = Application.platform.ToString(),
				Data = new Dictionary<string, string>
				{
					{"item_id", product.definition.id},
					{"fake_store", IsFakeStore(product).ToString()}
				}
			};

			_playfabService.CallFunction(request.Command, result =>
			{
				FLog.Info($"Purchase handled by the server: {product.definition.id}, {result.FunctionName}");

				var logicResult =
					JsonConvert.DeserializeObject<PlayFabResult<LogicResult>>(result.FunctionResult.ToString());
				var reward = ModelSerializer.DeserializeFromData<RewardData>(logicResult!.Result.Data);

				_commandService.ExecuteCommand(new AddIAPRewardCommand {Reward = reward});
				_commandService.ExecuteCommand(new CollectIAPRewardCommand());
				_store.ConfirmPendingPurchase(product);
			}, OnPlayFabError, request);
		}

		private void ValidateReceipt(Product product)
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

			PlayFabClientAPI.ValidateIOSReceipt(request, _ => PurchaseValidated(cacheProduct), OnPlayFabError);
#else
			var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
			var request = new ValidateGooglePlayPurchaseRequest
			{
				CurrencyCode = currencyCode,
				PurchasePrice = (uint) price,
				ReceiptJson = (string) data["json"],
				Signature = (string) data["signature"]
			};
			
			PlayFabClientAPI.ValidateGooglePlayPurchase(request, _ => PurchaseValidated(cacheProduct), GameCommandService.OnPlayFabError);
#endif
		}

		private void OnPlayFabError(PlayFabError error)
		{
			FLog.Error($"PlayFab error: {error.ErrorMessage}");
		}

		private bool IsFakeStore(Product product)
		{
			return !product.hasReceipt || string.IsNullOrEmpty(product.receipt) ||
				product.receipt.Contains("\"Store\":\"fake\"");
		}
	}
}