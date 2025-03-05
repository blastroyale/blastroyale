using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Commands.OfflineCommands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services.Analytics;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.Models;
using FirstLight.SDK.Services;
using FirstLightServerSDK.Services;
using I2.Loc;
using Newtonsoft.Json;
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

		public bool IsWeb3Purchase() 
		{
			var cost = PlayfabProductConfig.StoreItem.VirtualCurrencyPrices.FirstOrDefault();
			var gameid = PlayfabCurrencies.GetCurrency(cost.Key);
			return gameid.IsInGroup(GameIdGroup.CryptoCurrency);
		}
		
		public (GameId item, uint amt) GetPrice()
		{
			var cost = PlayfabProductConfig.StoreItem.VirtualCurrencyPrices.FirstOrDefault();

			return (PlayfabCurrencies.GetCurrency(cost.Key), cost.Value);
		}

		public string ID => PlayfabProductConfig.CatalogItem.ItemId;
	}

	/// <summary>
	/// Shop Category which is a division of products.
	/// This is derived from the "ItemClass" field in playfab
	/// </summary>
	public class GameProductCategory
	{
		public string Name;
		public List<GameProduct> Products = new ();

		public bool IsHidden => Name == "Hidden";
	}

	public class GameProductsBundle
	{
		public string Name;
		public GameProduct Bundle;
		public List<GameProduct> BundleProducts = new ();
	}

	/// <summary>
	/// Main entrypoint for our IAP service
	/// Integrates Unity Store & Playfab Store to allow IAP purchases
	/// </summary>
	public interface IIAPService
	{
		/// <summary>
		/// Api wrapper for Unity IAP integration. All operations are done via Unity Store.
		/// </summary>
		IUnityStoreService UnityStore { get; }

		/// <summary>
		/// Refences our remote catalog store
		/// </summary>
		IStoreService RemoteCatalogStore { get; }

		/// <summary>
		/// Buys the product defined by the given <paramref name="unityStoreId"/>
		/// </summary>
		void BuyProduct(GameProduct product);

		/// <summary>
		///  Check if there is a pending purchase in background, this is NOT reliable between restarts, you should handle when using this method.
		/// </summary>
		bool IsPending(GameProduct product);

		/// <summary>
		/// Gets all available products on the shop
		/// </summary>
		IReadOnlyCollection<GameProductCategory> AvailableProductCategories { get; }

		/// <summary>
		/// Gets all available products on the shop
		/// </summary>
		IReadOnlyCollection<GameProductsBundle> AvailableGameProductBundles { get; }

		/// <summary>
		/// Find GameProduct with ItemId
		/// </summary>
		IReadOnlyCollection<GameProduct> AvailableProducts { get; }

		/// <summary>
		/// Mark all the new store items as seen by the player
		/// </summary>
		void MarkAllNewStoreItemsAsSeen();

		/// <summary>
		/// Returns true if is there any new update on the store that player didn't see it
		/// </summary>
		bool HasStoreItemsUpdate(string filterCategory = null, string filterProduct = null);

		/// <summary>
		/// Event fired everytime a purchase finishes processing
		/// </summary>
		event PurchaseFinishedDelegate PurchaseFinished;

		delegate void PurchaseFinishedDelegate(string productId, ItemData data, bool succeeded, IUnityStoreService.PurchaseFailureData failureData);

		/// <summary>
		/// Display all the failed transactions popup and wait for it to close
		/// </summary>
		/// <returns></returns>
		public UniTask ShowQueuedTransactionFailedMessagesAndWait();
	}

	public class IAPService : IIAPService
	{
		/// <summary>
		/// This is the expected real income modifier. Stores takes 30% so our income is 70%.
		/// Only used for analytics purposes.
		/// </summary>
		private const decimal NET_INCOME_MODIFIER = (decimal) 0.7;

		private PlayfabStoreService _playfab;
		public event IIAPService.PurchaseFinishedDelegate PurchaseFinished;

		private Dictionary<string, GameProductCategory> _lastStoreLoadedDifference = new ();
		private Dictionary<string, GameProductCategory> _availableProductCategories = new ();
		private Dictionary<string, GameProductsBundle> _availableGameProductBundles = new ();
		private List<GameProduct> _availableProducts = new ();
		private UnityStoreService _unityStore;
		private readonly IGameCommandService _commandService;
		private readonly IGameBackendService _gameBackendService;
		private readonly IMessageBrokerService _msgBroker;
		private readonly IGameDataProvider _data;
		private readonly IHomeScreenService _homeScreen;
		private readonly LocalPrefsService _localPrefs;
		private readonly IGenericDialogService _genericDialogService;

		public List<Func<GameProduct, UniTask>> PrePurchaseHooks = new ();

		public IReadOnlyCollection<GameProductCategory> AvailableProductCategories => _availableProductCategories.Values;
		public IReadOnlyCollection<GameProductsBundle> AvailableGameProductBundles => _availableGameProductBundles.Values;
		public IReadOnlyCollection<GameProduct> AvailableProducts => _availableProducts;
		public IUnityStoreService UnityStore => _unityStore;
		public IStoreService RemoteCatalogStore => _playfab;

		private HashSet<string> _pending = new HashSet<string>();

		public IAPService(IGameCommandService commandService, IMessageBrokerService messageBroker,
						  IGameBackendService gameBackendService,
						  IGameDataProvider gameDataProvider, IHomeScreenService homeScreen, LocalPrefsService localPrefs,
						  IGenericDialogService genericDialogService)
		{
			_unityStore = new UnityStoreService(ProcessPurchase);
			_playfab = new PlayfabStoreService(gameBackendService, commandService, gameDataProvider);
			_commandService = commandService;
			_gameBackendService = gameBackendService;
			_msgBroker = messageBroker;
			_data = gameDataProvider;
			_homeScreen = homeScreen;
			_localPrefs = localPrefs;
			_genericDialogService = genericDialogService;
			_msgBroker.Subscribe<ShopScreenOpenedMessage>(OnShopOpened);
			_msgBroker.Subscribe<LogicInitializedMessage>(OnLogicInitialized);
			_unityStore.OnPurchaseFailure += OnPurchaseFailure;
			_homeScreen.RegisterNotificationQueueProcessor(OnHomeScreenNotification);
		}

		private void OnLogicInitialized(LogicInitializedMessage obj)
		{
			Init();
		}

		private async UniTask<IHomeScreenService.ProcessorResult> OnHomeScreenNotification(Type arg)
		{
			if (arg != typeof(HomeScreenService))
			{
				return IHomeScreenService.ProcessorResult.None;
			}

			var failed = _localPrefs.FailedTransactionMessages.Value;
			if (failed.Count == 0) return IHomeScreenService.ProcessorResult.None;
			// We need the store to get the product names
			await UniTask.WaitUntil(() => _unityStore.Initialized);
			await ShowQueuedTransactionFailedMessagesAndWait();
			return IHomeScreenService.ProcessorResult.None;
		}

		public string BuildMessage(IUnityStoreService.PurchaseFailureData data)
		{
			var product = AvailableProducts.FirstOrDefault(product => product.ID == data.ProductId);
			if (product == null) return null;
			var name = product.UnityIapProduct().metadata.localizedTitle;
			if (LocalizationManager.TryGetTranslation($"UITStore/transaction_failed_${data.Reason.ToString()}", out var translation))
			{
				return name + ": " + translation;
			}

			return name + ": " + data.Reason;
		}

		public bool ShouldDisplay(IUnityStoreService.PurchaseFailureData data)
		{
			return data.Reason != PurchaseFailureReason.ExistingPurchasePending && data.Reason != PurchaseFailureReason.UserCancelled;
		}

		public async UniTask ShowQueuedTransactionFailedMessagesAndWait()
		{
			var failed = _localPrefs.FailedTransactionMessages.Value;
			if (failed.Count == 0) return;
			await ShowAndWaitFailurePopup(_localPrefs.FailedTransactionMessages.Value.Values);
			_localPrefs.FailedTransactionMessages.Value = new Dictionary<string, IUnityStoreService.PurchaseFailureData>();
		}

		public async UniTask ShowAndWaitFailurePopup(IEnumerable<IUnityStoreService.PurchaseFailureData> data)
		{
			string allMessages = "";
			foreach (var failed in data)
			{
				if (!ShouldDisplay(failed)) continue;
				var msg = BuildMessage(failed);
				if (msg == null) continue;
				if (!string.IsNullOrEmpty(allMessages))
				{
					allMessages += "\n";
				}

				allMessages += msg;
			}

			_localPrefs.FailedTransactionMessages.Value = new Dictionary<string, IUnityStoreService.PurchaseFailureData>();

			if (string.IsNullOrEmpty(allMessages))
			{
				return;
			}

			var completionSource = new UniTaskCompletionSource();
			await _genericDialogService.OpenSimpleMessageAndWait(ScriptLocalization.UITStore.transaction_failed_popup_title, allMessages);
			await completionSource.Task;
		}

		public bool IsPending(GameProduct product)
		{
			return _pending.Contains(product.ID);
		}

		private void OnPurchaseFailure(IUnityStoreService.PurchaseFailureData data)
		{
			FLog.Warn("OnPurchaseFailure "+data.Reason+" "+data.Message);
			var reason = data.Reason;
			if (reason != PurchaseFailureReason.UserCancelled && reason != PurchaseFailureReason.ExistingPurchasePending)
			{
				SetQueuedFailedMessage(data);
			}

			if (reason == PurchaseFailureReason.ExistingPurchasePending) // If there is already a pending one lets block user from trying again
			{
				_pending.Add(data.ProductId);
			}
			else
			{
				_pending.Remove(data.ProductId);
			}

			PurchaseFinished?.Invoke(null, null, false, data);
		}

		public void SetQueuedFailedMessage(IUnityStoreService.PurchaseFailureData data)
		{
			var localPrefsFailed = _localPrefs.FailedTransactionMessages.Value;

			localPrefsFailed[data.ProductId] = data;

			_localPrefs.FailedTransactionMessages.Value = localPrefsFailed;
		}

		private void OnShopOpened(ShopScreenOpenedMessage msg)
		{
			if (_homeScreen.ForceBehaviour == HomeScreenForceBehaviourType.Store)
			{
				_homeScreen.SetForceBehaviour(HomeScreenForceBehaviourType.None);
			}

			TryUpdateStoreCatalog();
		}

		public void Init()
		{
			_playfab.Init();
			_playfab.OnStoreLoaded += (playfabProducts, playfabBundles) =>
			{
				_unityStore.InitializeUnityCatalog(playfabProducts
					.Select(i => i.CatalogItem.ItemId)
					.Union(playfabBundles?.Keys.Select(i => i.CatalogItem.ItemId) ?? Array.Empty<string>())
					.ToHashSet());

				ResolveCategoryProducts(playfabProducts);
				ResolveBundles(playfabBundles);
				UpdateLocalStoreState();
			};
		}

		private void UpdateLocalStoreState()
		{
			var lastStoreState = _localPrefs.LastStoreState;
			var newStoreState = new LocalStoreStateData();

			var isFirstTimeLoadingStore = lastStoreState.Value.Categories.Count == 0;

			foreach (var productCategory in _availableProductCategories.Values.Where(apc => !apc.IsHidden))
			{
				//No previous state persisted inside PlayerPrefs, All items and Categories will be tagged as new.
				if (isFirstTimeLoadingStore)
				{
					newStoreState.Categories.Add(new LocalStoreCategory()
					{
						CategoryName = productCategory.Name,
						Products = productCategory.Products.Select(p => new LocalStoreProduct
							{ProductName = p.PlayfabProductConfig.CatalogItem.ItemId, IsNewStoreItem = false}).ToList()
					});

					continue;
				}

				//Previous State has been found, try to get LastStateCategory
				var lastStoreStateCategory = lastStoreState.Value.Categories.FirstOrDefault(c => c.CategoryName == productCategory.Name);

				if (lastStoreStateCategory == null)
				{
					newStoreState.Categories.Add(new LocalStoreCategory()
					{
						CategoryName = productCategory.Name,
						Products = productCategory.Products.Select(p => new LocalStoreProduct
							{ProductName = p.PlayfabProductConfig.CatalogItem.ItemId, IsNewStoreItem = true}).ToList()
					});
				}
				else
				{
					var addedProducts = productCategory.Products
						.Where(productFromStore => lastStoreStateCategory.Products.All(lastStateProduct =>
							lastStateProduct.ProductName != productFromStore.PlayfabProductConfig.CatalogItem.ItemId))
						.Select(currentProduct => new LocalStoreProduct
						{
							ProductName = currentProduct.PlayfabProductConfig.CatalogItem.ItemId,
							IsNewStoreItem = true
						}).ToList();

					var removedProducts = lastStoreStateCategory.Products
						.Where(oldProduct => productCategory.Products.All(currentProduct =>
							currentProduct.PlayfabProductConfig.CatalogItem.ItemId != oldProduct.ProductName))
						.ToList();

					//Remove what need to be removed, Add what need to be added and keep everything else untouched
					lastStoreStateCategory.Products.RemoveAll(p => removedProducts.Contains(p));
					lastStoreStateCategory.Products.AddRange(addedProducts);

					newStoreState.Categories.Add(lastStoreStateCategory);
				}
			}

			_localPrefs.LastStoreState.Value = newStoreState;
		}

		public void MarkAllNewStoreItemsAsSeen()
		{
			var lastStoreState = _localPrefs.LastStoreState.Value;
			lastStoreState.Categories.ForEach(c => c.MarkProductsAsSeen());

			_localPrefs.LastStoreState.Value = lastStoreState;
		}

		public bool HasStoreItemsUpdate(string filterCategory = null, string filterProduct = null)
		{
			if (filterProduct != null)
			{
				return _localPrefs.LastStoreState.Value.Categories
					.Where(category => category?.Products != null)
					.SelectMany(category => category.Products)
					.Any(product => product?.ProductName == filterProduct && product.IsNewStoreItem);
			}

			if (filterCategory != null)
			{
				return _localPrefs.LastStoreState.Value.Categories
					.FirstOrDefault(c => c.CategoryName == filterCategory)
					?.HasUpdate() ?? false;
			}

			return _localPrefs.LastStoreState.Value.Categories.Any(c => c.HasUpdate());
		}

		private void ResolveBundles(Dictionary<PlayfabProductConfig, List<PlayfabProductConfig>> playfabGameProductBundles)
		{
			_availableGameProductBundles.Clear();

			foreach (var playfabBundle in playfabGameProductBundles)
			{
				var bundleId = playfabBundle.Key.CatalogItem.ItemId;

				//Resolve Bundle
				if (!_availableGameProductBundles.TryGetValue(bundleId, out var gameProductBundle))
				{
					gameProductBundle = new GameProductsBundle()
					{
						Name = bundleId,
						Bundle = new GameProduct()
						{
							GameItem = ItemFactory.PlayfabCatalog(playfabBundle.Key.CatalogItem),
							PlayfabProductConfig = playfabBundle.Key,
							UnityIapProduct = () => _unityStore.GetUnityProduct(playfabBundle.Key.CatalogItem.ItemId)
						}
					};
					_availableGameProductBundles[bundleId] = gameProductBundle;
				}

				//Resolve Bundle Products
				foreach (var playfabProduct in playfabBundle.Value)
				{
					gameProductBundle.BundleProducts.Add(new GameProduct()
					{
						GameItem = ItemFactory.PlayfabCatalog(playfabProduct.CatalogItem),
						PlayfabProductConfig = playfabProduct,
						UnityIapProduct = () => _unityStore.GetUnityProduct(playfabProduct.CatalogItem.ItemId)
					});
				}
			}
		}

		private void ResolveCategoryProducts(List<PlayfabProductConfig> playfabProducts)
		{
			foreach (var categoryList in _availableProductCategories.Values)
			{
				categoryList.Products.Clear();
			}

			foreach (var playfabProduct in playfabProducts)
			{
				var category = string.IsNullOrEmpty(playfabProduct.StoreItemData.Category) ? "General" : playfabProduct.StoreItemData.Category;
				if (!_availableProductCategories.TryGetValue(category, out var categoryList))
				{
					categoryList = new GameProductCategory() {Name = category};
					_availableProductCategories[category] = categoryList;
				}

				var gameProduct = new GameProduct()
				{
					GameItem = ItemFactory.PlayfabCatalog(playfabProduct.CatalogItem),
					PlayfabProductConfig = playfabProduct,
					UnityIapProduct = () => _unityStore.GetUnityProduct(playfabProduct.CatalogItem.ItemId)
				};

				categoryList.Products.Add(gameProduct);
				_availableProducts.Add(gameProduct);
			}
		}

		private void TryUpdateStoreCatalog()
		{
			_playfab.TryLoadStore();
		}

		public void BuyProduct(GameProduct product)
		{
			FLog.Verbose("Buy prodyct called");
			if (!this.IsRealMoney(product))
			{
				LogicPurchaseItem(product);
			}
			else
			{
				FLog.Info("IAP", $"Purchase initiated: {product.UnityIapProduct().definition.id}");
				_pending.Add(product.ID);
				_unityStore.Controller.InitiatePurchase(product.UnityIapProduct().definition.id);
			}
		}

		public async UniTask ConfirmLogicalPurchase(GameProduct product, ItemData item, (GameId item, uint price) price)
		{
			FLog.Info("IAP", "Purchase of logical item");
			foreach (var hook in PrePurchaseHooks)
			{
				await hook(product);
			}
			_commandService.ExecuteCommand(new BuyFromStoreCommand()
			{
				CatalogItemId = product.PlayfabProductConfig.CatalogItem.ItemId,
				StoreItemData = product.PlayfabProductConfig.StoreItemData
			});
			_pending.Remove(product.ID);
			FLog.Info("IAP", "Purchase finished, calling events");
			PurchaseFinished?.Invoke(product.ID, item, true, null);
		}

		private bool ShouldUseTextConfirmation(ItemData item)
		{
			if (!item.TryGetViewModel(out _))
			{
				return true;
			}

			if (item.HasMetadata<UnlockMetadata>())
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// A logical purchase happens when no real money (FIAT) is involved
		/// </summary>
		private void LogicPurchaseItem(GameProduct product)
		{
			var generatedItem = ItemFactory.PlayfabCatalog(product.PlayfabProductConfig.CatalogItem);
			var price = product.GetPrice();

			var shouldUseText = ShouldUseTextConfirmation(generatedItem);

			FLog.Verbose("Logic Purchase Item - should use text ? "+shouldUseText);
			
			GenericPurchaseDialogPresenter.IPurchaseData screenData = !shouldUseText
				? new GenericPurchaseDialogPresenter.IconPurchaseData()
				{
					Item = generatedItem,
					Value = price.amt,
					Currency = price.item,
					OnConfirm = () =>
					{
						FLog.Warn("Confirmed purchase");
						ConfirmLogicalPurchase(product, generatedItem, price).Forget();
					},
					OnExit = (shouldSeeShop) =>
					{
						_pending.Remove(product.ID);
						FLog.Warn("Closed purchase popup");
						PurchaseFinished?.Invoke(product.ID, generatedItem, false, new IUnityStoreService.PurchaseFailureData()
						{
							Reason = PurchaseFailureReason.UserCancelled,
							RequiredtoSeeShop = shouldSeeShop,
							ProductId = product.ID
						});
					}
				}
				: new GenericPurchaseDialogPresenter.TextPurchaseData()
				{
					TextFormat = ScriptLocalization.UITStore.logical_purchase_popup_text,
					Price = ItemFactory.Currency(price.item, (int) price.amt),
					OnConfirm = () => ConfirmLogicalPurchase(product, generatedItem, price).Forget(),
					OnExit = (shouldSeeShop) =>
					{
						FLog.Warn("Closed purchase popup 2");
						PurchaseFinished?.Invoke(product.ID, generatedItem, false, new IUnityStoreService.PurchaseFailureData()
						{
							RequiredtoSeeShop = shouldSeeShop,
							Reason = PurchaseFailureReason.UserCancelled,
							ProductId = product.ID
						});
					}
				};
			MainInstaller.ResolveServices().GenericDialogService.OpenPurchaseOrNotEnough(screenData);
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
			if (purchaseEvent.purchasedProduct.definition.id == BattlePassService.PRODUCT_ID_RM)
			{
				// Check if the user already have the BP, they could have bought with ingame currency while transaction was processing
				if (_data.BattlePassDataProvider.HasPurchasedSeason() || _data.RewardDataProvider.HasUnclaimedRewardWithId(GameId.PremiumBattlePass))
				{
					// I couldn't find a way to cancel the purchase process, so it will keep returning pending for 3 days then user will get refunded
					return PurchaseProcessingResult.Pending;
				}
			}

			FLog.Info("IAP",
				$"Purchase processed: {product.definition.id}, Dev store: {devStore}, TransactionId({purchaseEvent.purchasedProduct.transactionID})");
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
			// This game logic request will trigger the analytics events in the backend
			// This is all handled inside GameLogic's ShopService
			_playfab.AskBackendForItem(product, OnServerRewardConfirmed);
		}

		private void OnServerRewardConfirmed(Product product, ItemData item)
		{
			// The first command (client only) syncs up client state with the server, as the
			// server adds the reward item to UnclaimedRewards on its end, and we have to do the same,
			//TODO Doc
			if (item.Id == GameId.Bundle)
			{
				//TODO I might need to add some extras check for this one since we dont filter for expired bundles.
				_availableGameProductBundles.TryGetValue(product.definition.id, out var gameProductsBundle);

				if (gameProductsBundle != null)
				{
					var bundleRewards = new List<ItemData>();

					foreach (var bundleProduct in gameProductsBundle.BundleProducts)
					{
						bundleRewards.Add(bundleProduct.GameItem);
					}

					_commandService.ExecuteCommand(new AddIAPBundleRewardLocalCommand() {BundleRewards = bundleRewards.ToArray()});
				}
			}
			else
			{
				_commandService.ExecuteCommand(new AddIAPRewardLocalCommand {Reward = item});
			}

			_unityStore.Controller.ConfirmPendingPurchase(product);

			var availableProduct = _availableProducts.FirstOrDefault(p => p.PlayfabProductConfig.CatalogItem.ItemId.Equals(product.definition.id));

			if (availableProduct == null)
			{
				FLog.Warn("IAP",
					"Product has been purchased but was not found inside _availableProducts for fetching StoreItemData, it's not possible to track the item");
			}
			else
			{
				_commandService.ExecuteCommand(new UpdatePlayerStoreDataCommand()
				{
					CatalogItemId = product.definition.id,
					StoreItemData = availableProduct.PlayfabProductConfig.StoreItemData
				});
			}

			_pending.Remove(product.definition.id);
			PurchaseFinished?.Invoke(product.definition.id, item, true, null);

			SingularSDK.InAppPurchase(product, null);
		}
	}

	public static class IAPHelpers
	{
		private static HashSet<string> _handled = new (); // TODO: remove this
		private const int WAIT_FOR_TRANSACTION_SECONDS = 10;

		public static bool IsUIBeingHandled(string itemID)
		{
			return _handled.Contains(itemID);
		}

		public static bool IsRealMoney(this IIAPService iapService, GameProduct product)
		{
			//TODO Might not need to add
			return product.PlayfabProductConfig.StoreItem != null
				? product.PlayfabProductConfig.StoreItem.VirtualCurrencyPrices.Keys.Contains("RM")
				: product.PlayfabProductConfig.CatalogItem.VirtualCurrencyPrices.Keys.Contains("RM");
		}

		public enum BuyProductResult
		{
			ForcePlayerToShop,
			Rewarded,
			Rejected,
			Deferred,
		}

		/// <summary>
		/// Opens and wait for the whole purchase flow to end and returns the result
		/// This handles: Opening the Purchase Popup, Showing the Reward screen if successfull, Showing Transaction Error popup if not
		/// </summary>
		public static async UniTask<BuyProductResult> BuyProductHandleUI(this IIAPService service,
																		 GameProduct product,
																		 UIService.UIService uiService,
																		 IRewardService rewardService,
																		 IGenericDialogService genericDialogService
		)
		{
			FLog.Verbose("BuyProductHandleUI");
			var productId = product.PlayfabProductConfig.CatalogItem.ItemId;
			
			_handled.Add(productId);

			var purchaseFinishTaskSource =
				new UniTaskCompletionSource<(ItemData data, bool succeeded, IUnityStoreService.PurchaseFailureData failureData)>();
			var purchaseFinishTask = purchaseFinishTaskSource.Task;
			
			// Logical purchases there is nothing sketchy with UI because everything is done in the client so we don't need to wait
			if (!service.IsRealMoney(product))
			{
				FLog.Verbose("Handling purchase like real money or web3");
				service.PurchaseFinished += OnPurchaseFinished;
				service.BuyProduct(product);
				var logicPurchaseResult = await purchaseFinishTask;
				if (!logicPurchaseResult.succeeded)
				{
					FLog.Verbose("Logic Purchase Failed "+JsonConvert.SerializeObject(logicPurchaseResult.failureData));
					if (logicPurchaseResult.failureData.RequiredtoSeeShop)
					{
						return BuyProductResult.ForcePlayerToShop;
					}

					_handled.Remove(productId); 
					return BuyProductResult.Rejected;
				}

				// funfou passou aqui
				FLog.Verbose("Logic Purchase Succeeded");
				// For InGameCurrency it's claimed automatically so lets show reward screen
				service.PurchaseFinished -= OnPurchaseFinished;
				await rewardService.OpenRewardScreen(logicPurchaseResult.data);
				_handled.Remove(productId);
				return BuyProductResult.Rewarded;
			}

			// IAP HELL
			// Open the loading screen so the user doesn't do anything else in the mean time
			await uiService.OpenScreen<LoadingSpinnerScreenPresenter>();
			service.PurchaseFinished += OnPurchaseFinished;
			// Trigger IAP purchase
			service.BuyProduct(product);
			var delayTask = UniTask.Delay(TimeSpan.FromSeconds(WAIT_FOR_TRANSACTION_SECONDS));
			var result = await UniTask.WhenAny(purchaseFinishTask, delayTask);
			service.PurchaseFinished -= OnPurchaseFinished;
			await uiService.CloseScreen<LoadingSpinnerScreenPresenter>();
			if (result.hasResultLeft) // Purchase finished
			{
				if (result.result.succeeded) // Suceeded
				{
					FLog.Verbose("Purchase suceeded, claiming rewards");
					var opened = await rewardService.ClaimRewardsAndWaitForRewardsScreenToClose(result.result.data);
					_handled.Remove(productId);
					return BuyProductResult.Rewarded;
				}

				if (result.result.failureData.Reason == PurchaseFailureReason.ExistingPurchasePending)
				{
					_handled.Remove(productId);
					return BuyProductResult.Deferred;
				}

				await service.ShowQueuedTransactionFailedMessagesAndWait();
				_handled.Remove(productId);
				return BuyProductResult.Rejected;
			}

			// Purchase timeout, lets unblock UI and tell user purchase is being processed
			await genericDialogService.OpenSimpleMessage(ScriptLocalization.UITStore.deferred_transaction_popup_title,
				ScriptLocalization.UITStore.deferred_transaction_popup_desc);
			_handled.Remove(productId);
			return BuyProductResult.Deferred;

			void OnPurchaseFinished(string itemId, ItemData data, bool succeeded, IUnityStoreService.PurchaseFailureData failureData)
			{
				purchaseFinishTaskSource.TrySetResult((data, succeeded, failureData));
			}
		}
	}
}