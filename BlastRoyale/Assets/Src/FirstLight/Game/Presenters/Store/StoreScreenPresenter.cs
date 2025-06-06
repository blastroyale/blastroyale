using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Presenters.Store
{
	/// <summary>
	/// Manages the IAP store.
	/// </summary>
	public class StoreScreenPresenter : UIPresenterData<StoreScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		public const string USS_CATEGORY = "product-category";
		public const string USS_CATEGORY_LABEL = "category-label";
		public const string USS_CATEGORY_BUTTON = "category-button";
		public const string USS_NOTIFICATION_ICON = "notification-icon";

		[SerializeField] private VisualTreeAsset _StoreProductView;

		private IGameServices _gameServices;
		private IGameDataProvider _data;

		private VisualElement _blocker;
		private ScreenHeaderElement _header;
		private VisualElement _productList;
		private ScrollView _categoryList;
		private ScrollView _scroll;
		private Dictionary<string, VisualElement> _categoriesElements = new ();

		private void Awake()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();
			_data = MainInstaller.ResolveData();
		}

		protected override void QueryElements()
		{
			_blocker = Root.Q("Blocker").Required();

			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			_productList = Root.Q("ProductList").Required();
			_categoryList = Root.Q<ScrollView>("Categories").Required();
			_scroll = Root.Q<ScrollView>("ProductScrollView").Required();
			_header.backClicked = Data.OnBackClicked;

			Root.Q<CryptoCurrenciesDisplayElement>("CryptoCurrency").AttachView(this, out CryptoCurrenciesDisplayView _);
			Root.Q<CurrencyDisplayElement>("Coins").AttachView(this, out CurrencyDisplayView _);
			Root.Q<CurrencyDisplayElement>("BlastBucks").AttachView(this, out CurrencyDisplayView _);

			LoadStore();
		}

		private void LoadStore()
		{
			_categoriesElements.Clear();

			foreach (var category in _gameServices.IAPService.AvailableProductCategories)
			{
				if (category.IsHidden)
				{
					continue;
				}

				var categoryElement = new StoreCategoryElement(category.Name);

				foreach (var product in category.Products)
				{
					var productElement = new StoreGameProductElement();

					categoryElement.Add(productElement);
					categoryElement.EnsureSize(product.PlayfabProductConfig.StoreItemData.Size);

					var trackedPurchasedItem = GetTrackedPurchasedItem(product);
					var flags = ProductFlags.NONE;

					if (IsItemOwned(product))
					{
						flags |= ProductFlags.OWNED;
					}
					else
					{

						if (IsBuyAllowed(product))
						{
							productElement.OnClicked = BuyItem;
						}
						else
						{
							productElement.OnClicked = ShowBuyNotAllowedNotification;

							flags = SetupBuyNotAllowedFlags(product, flags);
						}
					}

					var isNewItem = IsNewOnStore(product);
					productElement.SetData(product, flags, trackedPurchasedItem, isNewItem, BuyItem, Root);
				}

				_productList.Add(categoryElement);

				var categoryButton = CreateCategoryButton(category.Name, categoryElement);
				_categoryList.Add(categoryButton);
				_categoriesElements[category.Name] = categoryElement;

				//Resize Category Element Container after adding all products to it.
				categoryElement.RegisterCallback<GeometryChangedEvent>(_ => categoryElement.ResizeContainer());
			}

			SetupCreatorsCodeSupport();
		}


		private ProductFlags SetupBuyNotAllowedFlags(GameProduct product, ProductFlags flags)
		{
			var trackedPurchasedItem = GetTrackedPurchasedItem(product);

			//Should have a better mechanism to prioritize what are the orders of notification
			if (HasMaxAmountReached(trackedPurchasedItem, product.PlayfabProductConfig.StoreItemData))
			{
				flags |= ProductFlags.MAXAMOUNT;
			}
			else if (!HasPurchaseCooldownExpired(trackedPurchasedItem.LastPurchaseTime, product.PlayfabProductConfig.StoreItemData.PurchaseCooldown))
			{
				flags |= ProductFlags.COOLDOWN;
			}

			return flags;
		}

		private void ShowBuyNotAllowedNotification(GameProduct product)
		{
			var trackedPurchasedItem = GetTrackedPurchasedItem(product);

			//Should have a better mechanism to prioritize what are the orders of notification
			if (HasMaxAmountReached(trackedPurchasedItem, product.PlayfabProductConfig.StoreItemData))
			{
				if (trackedPurchasedItem.AmountPurchased >= product.PlayfabProductConfig.StoreItemData.MaxAmount)
				{
					_gameServices.InGameNotificationService.QueueNotification(ScriptLocalization.UITStore.notification_product_daily_reset);
				}
				else
				{
					_gameServices.InGameNotificationService.QueueNotification(ScriptLocalization.UITStore.notification_product_maxamount);
				}
			}
			else if (!HasPurchaseCooldownExpired(trackedPurchasedItem.LastPurchaseTime, product.PlayfabProductConfig.StoreItemData.PurchaseCooldown))
			{
				_gameServices.InGameNotificationService.QueueNotification(ScriptLocalization.UITStore.notification_product_cooldown);
			}
		}

		private ButtonOutlined CreateCategoryButton(string categoryName, VisualElement categoryElement)
		{
			var categoryButton = new ButtonOutlined(categoryName, () => SelectCategory(categoryElement))
			{
				name = "CategoryButton",
			};
			
			categoryButton.AddToClassList(USS_CATEGORY_BUTTON);

			if (_gameServices.IAPService.HasStoreItemsUpdate(categoryName))
			{
				var updateNotification = new VisualElement()
				{
					name = "CategoryNotification"
				};
				
				updateNotification.AddToClassList(USS_NOTIFICATION_ICON);
				categoryButton.RegisterCallback<GeometryChangedEvent>(ResolveCategoryNewNotificationPosition);
				categoryButton.Add(updateNotification);
				
			}

			return categoryButton;
		}

		private void ResolveCategoryNewNotificationPosition(GeometryChangedEvent evt)
		{
			var categoryButton = (ButtonOutlined)evt.currentTarget;
			var categoryNotification = categoryButton.Children().FirstOrDefault(e => e.name == "CategoryNotification");

			if (categoryNotification != null)
			{
				var textSize = categoryButton.MeasureTextSize(categoryButton.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
				
				categoryNotification.style.position = Position.Absolute;
				categoryNotification.style.left = (categoryButton.resolvedStyle.width + textSize.x) / 2 - 10; // Using a 10px offset to slightly shift the notification icon over the text for better visual alignment.
				categoryNotification.style.bottom = (categoryButton.resolvedStyle.height + textSize.y) / 5;
			}
		}

		
		private bool IsNewOnStore(GameProduct product)
		{
			return _gameServices.IAPService.HasStoreItemsUpdate(null, product.PlayfabProductConfig.CatalogItem.ItemId);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_gameServices.MessageBrokerService.Subscribe<OpenedCoreMessage>(OnCoresOpened);
			_gameServices.IAPService.PurchaseFinished += OnPurchaseFinished;

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			_gameServices.MessageBrokerService.UnsubscribeAll(this);
			_gameServices.IAPService.PurchaseFinished -= OnPurchaseFinished;
			_gameServices.IAPService.MarkAllNewStoreItemsAsSeen();
			return base.OnScreenClose();
		}

		/// <summary>
		/// Checks if its already owned and should not allow double purchase
		/// </summary>
		private bool IsItemOwned(GameProduct product)
		{
			if (!product.GameItem.Id.IsInGroup(GameIdGroup.Collection)) return false;
			return _data.CollectionDataProvider.IsItemOwned(product.GameItem);
		}

		private bool IsBuyAllowed(GameProduct product)
		{
			var storeItemData = product.PlayfabProductConfig.StoreItemData;

			if (HasCooldownConfiguration(storeItemData) | HasMaxAmountConfiguration(storeItemData))
			{
				var trackedPurchasedItem = GetTrackedPurchasedItem(product);

				if (trackedPurchasedItem != null)
				{
					return !HasMaxAmountReached(trackedPurchasedItem, storeItemData) &&
						HasPurchaseCooldownExpired(trackedPurchasedItem.LastPurchaseTime, storeItemData.PurchaseCooldown);
				}

				return true;
			}

			return true;
		}

		private StorePurchaseData GetTrackedPurchasedItem(GameProduct product)
		{
			return _data.PlayerStoreDataProvider.GetTrackedPlayerPurchases()
				.Find(m => m.CatalogItemId.Equals(product.PlayfabProductConfig.CatalogItem.ItemId));
		}

		private static bool HasMaxAmountReached(StorePurchaseData trackedPurchasedItem, StoreItemData storeItemData)
		{
			return trackedPurchasedItem.AmountPurchased >= storeItemData.MaxAmount;
		}

		private bool HasPurchaseCooldownExpired(DateTime lastPurchaseTime, int cooldownMinutes)
		{
			var timeSinceLastAction = DateTime.UtcNow - lastPurchaseTime;

			return timeSinceLastAction.TotalMinutes >= cooldownMinutes;
		}

		private bool HasCooldownConfiguration(StoreItemData storeItemData)
		{
			return storeItemData.PurchaseCooldown > 0;
		}

		private bool HasMaxAmountConfiguration(StoreItemData storeItemData)
		{
			return storeItemData.MaxAmount > 0;
		}

		private void SelectCategory(VisualElement categoryContainer)
		{
			var targetX = categoryContainer.resolvedStyle.left;
			_scroll.experimental.animation.Start(0, 1f, 300, (element, percent) =>
			{
				var scrollView = (ScrollView) element;
				var currentScroll = scrollView.scrollOffset;
				scrollView.scrollOffset = new Vector2(targetX * percent, currentScroll.y);
			}).Ease(Easing.OutCubic);
		}

		public void GoToCategoryWithProduct(GameId id)
		{
			foreach (var category in _gameServices.IAPService.AvailableProductCategories)
			{
				if (category.Products.Any(a => a.GameItem.Id == id))
				{
					SelectCategory(_categoriesElements[category.Name]);
					return;
				}
			}
		}

		/// <summary>
		/// This is here in case the purchase takes longer then expected and the player is still on the screen when getting the reward
		/// </summary>
		private void OnPurchaseFinished(string itemId, ItemData item, bool success, IUnityStoreService.PurchaseFailureData reason)
		{
			if (!success) return;
			if (IAPHelpers.IsUIBeingHandled(itemId))
			{
				FLog.Verbose("UI being handled by IAP handler");
				return;
			}

			_gameServices.RewardService.ClaimRewardsAndWaitForRewardsScreenToClose(item).ContinueWith((openedRewards) =>
			{
				if (!openedRewards) return;
				_gameServices.UIService.OpenScreen<StoreScreenPresenter>(Data).Forget();
			});
		}

		private void OnCoresOpened(OpenedCoreMessage msg)
		{
			FLog.Verbose("Store Screen", $"Viewing Opening Core {msg.Core}");
			_gameServices.UIService.OpenScreen<RewardsScreenPresenter>(
				new RewardsScreenPresenter.StateData()
				{
					ParentItem = msg.Core,
					Items = msg.Results,
					FameRewards = false,
					OnFinish = () =>
					{
						_gameServices.UIService.OpenScreen<StoreScreenPresenter>(Data).Forget();
					}
				}).Forget();
		}

		private void BuyItem(GameProduct product)
		{
			_gameServices.IAPService.BuyProductHandleUI(product,
				_gameServices.UIService,
				_gameServices.RewardService,
				_gameServices.GenericDialogService).ContinueWith(result =>
			{
				if (result != IAPHelpers.BuyProductResult.Rewarded) return;
				_gameServices.UIService.OpenScreen<StoreScreenPresenter>(Data).Forget();
			});
		}

		//Content Creator
		private void SetupCreatorsCodeSupport()
		{
			var contentCreatorElement = InstantiateCreatorCodeVisualElement();

			contentCreatorElement.OnEnterCodeClicked = OpenEnterCreatorCodePopup;
			contentCreatorElement.OnUpdateCodeClicked = OpenEnterCreatorCodePopup;
			contentCreatorElement.OnStopSupportingClicked = OpenStopSupportingCreatorPopup;

			contentCreatorElement.SetData(_data.ContentCreatorDataProvider.SupportingCreatorCode.Value);
			_data.ContentCreatorDataProvider.SupportingCreatorCode.Observe(contentCreatorElement.UpdateContentCreator);
		}

		private StoreCreatorCodeElement InstantiateCreatorCodeVisualElement()
		{
			var contentCreatorLabel = ScriptLocalization.UITStore.content_creator.ToUpperInvariant();

			var contentCreatorElement = new StoreCreatorCodeElement();
			var categoryElement = new StoreCategoryElement(contentCreatorLabel);
			categoryElement.Add(contentCreatorElement);

			_productList.Add(categoryElement);

			var categoryButton = CreateCategoryButton(contentCreatorLabel, categoryElement);
			_categoryList.Add(categoryButton);
			_categoriesElements[contentCreatorLabel] = categoryElement;

			return contentCreatorElement;
		}

		private bool IsValidCreatorCode(string creatorCode)
		{
			if (_gameServices.GameAppService.AppData.TryGetValue("ACTIVE_CREATORS_CODE", out var activeCreatorsCode))
			{
				if (!string.IsNullOrEmpty(activeCreatorsCode))
				{
					return activeCreatorsCode.Split(",").Contains(creatorCode);
				}
			}

			return false;
		}

		private void OpenEnterCreatorCodePopup()
		{
			PopupPresenter.OpenEnterCreatorCode(OnCreatorCodeSubmitted).Forget();
		}

		private void OpenStopSupportingCreatorPopup()
		{
			PopupPresenter.OpenGenericConfirm(ScriptTerms.UITStore.content_creator, ScriptLocalization.UITStore.content_creator_stop_supporting,
				OnStopSupportingCreatorSubmitted).Forget();
		}

		private void OnCreatorCodeSubmitted(string creatorCode)
		{
			var creatorCodeValue = creatorCode.ToUpperInvariant();

			if (!IsValidCreatorCode(creatorCodeValue))
			{
				PopupPresenter.OpenGenericInfo(ScriptTerms.UITStore.content_creator, ScriptLocalization.UITStore.content_creator_invalid_code)
					.Forget();
				return;
			}

			_gameServices.CommandService.ExecuteCommand(new SupportCreatorCommand() {CreatorCode = creatorCodeValue});
			PopupPresenter.ClosePopupScreen();
		}

		private void OnStopSupportingCreatorSubmitted()
		{
			_gameServices.CommandService.ExecuteCommand(new SupportCreatorCommand() {CreatorCode = string.Empty});
			PopupPresenter.ClosePopupScreen();
		}
	}
}