using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Commands.OfflineCommands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Models;
using FirstLight.SDK.Services;
using FirstLightServerSDK.Services;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine.Purchasing;


namespace FirstLight.Game.Services
{
	/// <summary>
	/// Wrapper product class that holds game specific data
	/// </summary>
	public class GameProduct
	{
		public PlayfabProductConfig PlayfabProductConfig;
		public Func<Product> UnityIapProduct; // TODO: Fix this properly, it shouldn't be a func
		public ItemData GameItem;

		public (GameId item, uint amt) GetPrice()
		{
			var cost = PlayfabProductConfig.StoreItem.VirtualCurrencyPrices.First();
			return (PlayfabCurrencies.GetCurrency(cost.Key), cost.Value);
		}
	}
	
	/// <summary>
	/// Shop Category which is a division of products.
	/// This is derived from the "ItemClass" field in playfab
	/// </summary>
	public class GameProductCategory
	{
		public string Name;
		public List<GameProduct> Products = new();
	}
	
	/// <summary>
	/// Main entrypoint for our IAP service
	/// Integrates Unity Store & Playfab Store to allow IAP purchases
	/// </summary>
	public interface IIAPService
	{
		/// <summary>
		/// If flagged as needs to see store, the next time player opens main manu 
		/// </summary>
		bool RequiredToViewStore { get; set; }
		
		/// <summary>
		/// Api wrapper for Unity IAP integration. All operations are done via Unity Store.
		/// </summary>
		IUnityStoreSerivce UnityStore { get; }
		
		/// <summary>
		/// Refences our remote catalog store
		/// </summary>
		IStoreService RemoteCatalogStore { get; }
			
		/// <summary>
		/// Buys the product defined by the given <paramref name="unityStoreId"/>
		/// </summary>
		void BuyProduct(GameProduct product);
		
		/// <summary>
		/// Gets all available products on the shop
		/// </summary>
		IReadOnlyCollection<GameProductCategory> AvailableProductCategories { get; }

		/// <summary>
		/// Initializes the purchasing module.
		/// </summary>
		void Init();
		
		/// <summary>
		/// Event fired everytime a purchase finishes processing
		/// </summary>
		event Action<ItemData, bool> PurchaseFinished;
	}

	public class IAPService : IIAPService
	{
		/// <summary>
		/// This is the expected real income modifier. Stores takes 30% so our income is 70%.
		/// Only used for analytics purposes.
		/// </summary>
		private const decimal NET_INCOME_MODIFIER = (decimal)0.7;
		
		private PlayfabStoreService _playfab;

		public event Action<ItemData, bool> PurchaseFinished;
		
		private Dictionary<string, GameProductCategory> _availableProducts = new ();
		private UnityStoreService _unityStore;
		private readonly IGameCommandService _commandService;
		private readonly IGameBackendService _gameBackendService;
		private readonly IAnalyticsService _analyticsService;
		private readonly IMessageBrokerService _msgBroker;
		private readonly IGameDataProvider _data;
		
		public bool RequiredToViewStore { get; set; }
		
		public IReadOnlyCollection<GameProductCategory> AvailableProductCategories => _availableProducts.Values;
		public IUnityStoreSerivce UnityStore => _unityStore;
		public IStoreService RemoteCatalogStore => _playfab;
		public IAPService(IGameCommandService commandService, IMessageBrokerService messageBroker,
						  IGameBackendService gameBackendService, IAnalyticsService analyticsService,
						  IGameDataProvider gameDataProvider)
		{
			_unityStore = new UnityStoreService(ProcessPurchase);
			_playfab = new PlayfabStoreService(gameBackendService, commandService);
			_commandService = commandService;
			_gameBackendService = gameBackendService;
			_msgBroker = messageBroker;
			_analyticsService = analyticsService;
			_data = gameDataProvider;
			_msgBroker.Subscribe<ShopScreenOpenedMessage>(OnShopOpened);
		}

		private void OnShopOpened(ShopScreenOpenedMessage msg)
		{
			RequiredToViewStore = false;
		}

		public void Init()
		{
			_playfab.Init();
			_playfab.OnStoreLoaded += playfabProducts =>
			{
				_unityStore.InitializeUnityCatalog(playfabProducts.Select(i => i.CatalogItem.ItemId).ToHashSet());
				foreach (var playfabProduct in playfabProducts)
				{
					var category = string.IsNullOrEmpty(playfabProduct.StoreItemData.Category) ? "General" : playfabProduct.StoreItemData.Category;
					if (!_availableProducts.TryGetValue(category, out var categoryList))
					{
						categoryList = new GameProductCategory() { Name = category };
						_availableProducts[category] = categoryList;
					}
					categoryList.Products.Add(new GameProduct()
					{
						GameItem = ItemFactory.PlayfabCatalog(playfabProduct.CatalogItem),
						PlayfabProductConfig = playfabProduct,
						UnityIapProduct = () => _unityStore.GetUnityProduct(playfabProduct.CatalogItem.ItemId)
					});
				}
			};
		}

		private bool IsRealMoney(GameProduct product)
		{
			return product.PlayfabProductConfig.StoreItem.VirtualCurrencyPrices.Keys.Contains("RM");
		}

		public void BuyProduct(GameProduct product)
		{
			if (!IsRealMoney(product))
			{
				LogicPurchaseItem(product);
			}
			else
			{
				FLog.Info("IAP",$"Purchase initiated: {product.UnityIapProduct().definition.id}");
				_unityStore.Controller.InitiatePurchase(product.UnityIapProduct().definition.id);
			}
		}

		private void ConfirmLogicalPurchase(GameProduct product, ItemData item, (GameId item, uint price) price)
		{
			FLog.Info("IAP", "Purchase of logical item");
			_commandService.ExecuteCommand(new BuyFromStoreCommand()
			{
				CatalogItemId = product.PlayfabProductConfig.CatalogItem.ItemId
			});
			PurchaseFinished?.Invoke(item, true);
			_analyticsService.EconomyCalls.PurchaseIngameItem(product.UnityIapProduct(), item, price.item.ToString(), price.price);
		}

		/// <summary>
		/// A logical purchase happens when no real money (FIAT) is involved
		/// </summary>
		private void LogicPurchaseItem(GameProduct product)
		{
			var generatedItem = ItemFactory.PlayfabCatalog(product.PlayfabProductConfig.CatalogItem);
			var price = product.GetPrice();

			MainInstaller.ResolveServices().GenericDialogService.OpenPurchaseOrNotEnough(new ()
			{
				Item = generatedItem,
				Value = price.amt,
				Currency = price.item,
				OnConfirm = () => ConfirmLogicalPurchase(product, generatedItem, price),
				OnExit = () =>
				{
					PurchaseFinished?.Invoke(generatedItem, false);
				}
			});
		}

		/// <summary>
		/// This is called when Unity finishes processing the purchase.
		/// This is also called when application starts in case there was a pending transaction
		/// 
		/// https://docs.unity3d.com/Manual/UnityIAPProcessingPurchases.html
		/// </summary>
		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
		{
			var devStore = _gameBackendService.IsDev();
			var product = purchaseEvent.purchasedProduct;
			FLog.Info("IAP",$"Purchase processed: {product.definition.id}, Dev store: {devStore}, TransactionId({purchaseEvent.purchasedProduct.transactionID})");
			if (devStore) _playfab.AskBackendForItem(product, OnServerRewardConfirmed);
			else _playfab.ValidateReceipt(product, OnPurchaseValidated);
			return PurchaseProcessingResult.Pending;
		}

		/// <summary>
		/// This is called when Playfab validate the purchase Unity IAP SDK did
		/// After confirming in Unity SDK the purchase is flagged as "complete"
		/// </summary>
		private void OnPurchaseValidated(Product product)
		{
			_playfab.AskBackendForItem(product, OnServerRewardConfirmed);
		}

		private void OnServerRewardConfirmed(Product product, ItemData item)
		{
			// The first command (client only) syncs up client state with the server, as the
			// server adds the reward item to UnclaimedRewards on its end, and we have to do the same.
			_commandService.ExecuteCommand(new AddIAPRewardLocalCommand {Reward = item});

			// Second command is server and client, and collects the unclaimed reward.
			_commandService.ExecuteCommand(new ClaimUnclaimedRewardCommand()
			{
				ToClaim = item
			});
			_unityStore.Controller.ConfirmPendingPurchase(product);
			PurchaseFinished?.Invoke(item, true);
			SendAnalyticsEvent(product, item);
		}

		private void SendAnalyticsEvent(Product product, ItemData reward)
		{
			if (_gameBackendService.IsDev()) return;
			var price = product.metadata.localizedPrice;
			_analyticsService.EconomyCalls.Purchase(product, reward, price, NET_INCOME_MODIFIER);
		}
	}
}