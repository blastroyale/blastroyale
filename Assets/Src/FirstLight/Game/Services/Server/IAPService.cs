using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Commands.OfflineCommands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules;
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

		/// <summary>
		/// Initializes the purchasing module.
		/// </summary>
		void Init();
	}

	public class IAPService : IIAPService, IStoreListener
	{
		private const float NET_INCOME_MODIFIER = 0.7f;

		public IObservableFieldReader<bool> Initialized => _initialized;
		public IReadOnlyList<Product> Products => _products;

		private List<Product> _products;
		private readonly IObservableField<bool> _initialized;

		private readonly IGameCommandService _commandService;
		private readonly IMessageBrokerService _messageBroker;
		private readonly IGameBackendService _gameBackendService;
		private readonly IAnalyticsService _analyticsService;
		private readonly IGameDataProvider _gameDataProvider;

		private IStoreController _store;
		private ProductCatalog _defaultCatalog;

		public IAPService(IGameCommandService commandService, IMessageBrokerService messageBroker,
						  IGameBackendService gameBackendService, IAnalyticsService analyticsService,
						  IGameDataProvider gameDataProvider)
		{
			_commandService = commandService;
			_messageBroker = messageBroker;
			_gameBackendService = gameBackendService;
			_analyticsService = analyticsService;
			_gameDataProvider = gameDataProvider;

			_products = new List<Product>();
			_initialized = new ObservableField<bool>(false);
		}

		public void Init()
		{
			var module = StandardPurchasingModule.Instance();

#if !STORE_BUILD
			var useFakeStore = PlayerPrefs.GetInt("Debug.UseFakeStore", 1) == 1;
			var fakeStoreUiMode =
				Enum.Parse<FakeStoreUIMode>(PlayerPrefs.GetString("Debug.FakeStoreUiMode",
					FakeStoreUIMode.Default.ToString()));

			module.useFakeStoreAlways = useFakeStore;
			module.useFakeStoreUIMode = fakeStoreUiMode;
#endif

			var builder = ConfigurationBuilder.Instance(module);

			IAPConfigurationHelper.PopulateConfigurationBuilder(ref builder,
				_defaultCatalog = ProductCatalog.LoadDefaultCatalog());

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

			var pendingRewards = _store.products.set.Any(product =>
				!string.IsNullOrEmpty(product.receipt) && !string.IsNullOrEmpty(product.transactionID)
			);
			var unclaimedPurchases = _gameDataProvider.RewardDataProvider.HasUnclaimedPurchases();

			if (!pendingRewards && unclaimedPurchases)
			{
				_commandService.ExecuteCommand(new CollectIAPRewardCommand());
			}

			FLog.Info($"IAP Initialized: Pending({pendingRewards}), Unclaimed({unclaimedPurchases})");
		}

		public void OnInitializeFailed(InitializationFailureReason error)
		{
			OnInitializeFailed(error, null);
		}

		public void OnInitializeFailed(InitializationFailureReason error, string message)
		{
			FLog.Warn($"IAP Initialization failed: {error} - {message}");

			_initialized.Value = false;
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
		{
			var fakeStore = IsFakeStore(purchaseEvent.purchasedProduct);
			var validated = _gameDataProvider.RewardDataProvider.HasUnclaimedPurchase(purchaseEvent.purchasedProduct);

			FLog.Info(
				$"Purchase processed: {purchaseEvent.purchasedProduct.definition.id}, Fake store: {fakeStore}, Validated: {validated}, TransactionId({purchaseEvent.purchasedProduct.transactionID})");

			if (fakeStore)
			{
				PurchaseValidated(purchaseEvent.purchasedProduct);
			}
			else
			{
				if (validated)
				{
					PurchaseValidated(purchaseEvent.purchasedProduct);
				}
				else
				{
					ValidateReceipt(purchaseEvent.purchasedProduct);
				}
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

			_gameBackendService.CallFunction(request.Command, result =>
			{
				FLog.Info($"Purchase handled by the server: {product.definition.id}, {result.FunctionName}");

				var logicResult =
					JsonConvert.DeserializeObject<PlayFabResult<LogicResult>>(result.FunctionResult.ToString());
				var reward = ModelSerializer.DeserializeFromData<RewardData>(logicResult!.Result.Data);

				// The first command (client only) syncs up client state with the server, as the
				// server adds the reward item to UnclaimedRewards on its end, and we have to do the same.
				_commandService.ExecuteCommand(new AddIAPRewardLocalCommand {Reward = reward});

				// Second command is server and client, and collects the unclaimed reward.
				_commandService.ExecuteCommand(new CollectIAPRewardCommand());

				_store.ConfirmPendingPurchase(product);

				SendAnalyticsEvent(product, reward);
			}, null, request);
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

			PlayFabClientAPI.ValidateIOSReceipt(request, _ => PurchaseValidated(cacheProduct),
				e =>
				{
					_gameBackendService.HandleError(e,null, AnalyticsCallsErrors.ErrorType.Session);
				});
#else
			var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
			var request = new ValidateGooglePlayPurchaseRequest
			{
				CurrencyCode = currencyCode,
				PurchasePrice = (uint) price,
				ReceiptJson = (string) data["json"],
				Signature = (string) data["signature"]
			};
			
			PlayFabClientAPI.ValidateGooglePlayPurchase(request, _ => PurchaseValidated(cacheProduct),
				e =>
				{
					_gameBackendService.HandleError(e,null, AnalyticsCallsErrors.ErrorType.Session);
				});
#endif
		}

		private bool IsFakeStore(Product product)
		{
			return !product.hasReceipt || string.IsNullOrEmpty(product.receipt) ||
				product.receipt.Contains("\"Store\":\"fake\"");
		}

		private void SendAnalyticsEvent(Product product, RewardData reward)
		{
			if (IsFakeStore(product)) return;

			var catalogItem = _defaultCatalog.allProducts.First(item => item.id == product.definition.id);

			float price = (float) catalogItem.googlePrice.value;
			_analyticsService.EconomyCalls.Purchase(product, reward, price, NET_INCOME_MODIFIER);
		}
		// TODO - ADD PLAYFAB ERROR HANDLING IDENTIAL TO THE ONE IN GAME BACKEND NETWORK SERVICE
	}
}