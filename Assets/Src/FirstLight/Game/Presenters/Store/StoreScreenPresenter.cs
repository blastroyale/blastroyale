using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
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
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Button = UnityEngine.UIElements.Button;


namespace FirstLight.Game.Presenters.Store
{
	/// <summary>
	/// Manages the IAP store.
	/// </summary>
	public class StoreScreenPresenter : UIPresenterData2<StoreScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action<GameProduct> OnPurchaseItem;
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		public const string USS_CATEGORY = "product-category";
		public const string USS_CATEGORY_LABEL = "category-label";
		public const string USS_CATEGORY_BUTTON = "category-button";

		[SerializeField] private VisualTreeAsset _StoreProductView;

		private IGameServices _gameServices;
		private IGameDataProvider _data;

		private VisualElement _blocker;
		private ScreenHeaderElement _header;
		private VisualElement _productList;
		private VisualElement _categoryList;
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
			_categoryList = Root.Q("Categories").Required();
			_scroll = Root.Q<ScrollView>("ProductScrollView").Required();
			_header.backClicked += Data.OnBackClicked;

			Root.Q<CurrencyDisplayElement>("Coins")
				.AttachView2(this, out CurrencyDisplayView _);

			Root.Q<CurrencyDisplayElement>("BlastBucks")
				.AttachView2(this, out CurrencyDisplayView _);

			_categoriesElements.Clear();
			foreach (var category in _gameServices.IAPService.AvailableProductCategories)
			{
				var categoryElement = new StoreCategoryElement(category.Name);
				foreach (var product in category.Products)
				{
					var productElement = new StoreGameProductElement();

					categoryElement.Add(productElement);
					categoryElement.EnsureSize(product.PlayfabProductConfig.StoreItemData.Size);
					var flags = ProductFlags.NONE;
					if (IsItemOwned(product))
					{
						flags |= ProductFlags.OWNED;
					}
					else
					{
						productElement.OnClicked = BuyItem;
					}

					productElement.SetData(product, flags, Root);
				}

				_productList.Add(categoryElement);
				var categoryButton = new Button();
				categoryButton.text = category.Name;
				categoryButton.AddToClassList(USS_CATEGORY_BUTTON);
				categoryButton.clicked += () => SelectCategory(categoryElement);
				_categoryList.Add(categoryButton);
				_categoriesElements[category.Name] = categoryElement;
			}
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_gameServices.MessageBrokerService.Subscribe<OpenedCoreMessage>(OnCoresOpened);
			_gameServices.MessageBrokerService.Subscribe<ItemRewardedMessage>(OnItemRewarded);
			_gameServices.IAPService.UnityStore.OnPurchaseFailure += OnPurchaseFailed;
			_gameServices.IAPService.PurchaseFinished += OnPurchaseFinished;

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			_gameServices.MessageBrokerService.UnsubscribeAll(this);
			_gameServices.IAPService.UnityStore.OnPurchaseFailure -= OnPurchaseFailed;
			_gameServices.IAPService.PurchaseFinished -= OnPurchaseFinished;
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

		private void OnPurchaseFinished(ItemData item, bool success)
		{
			_blocker.style.display = DisplayStyle.None;
		}

		[Button]
		private void OnPurchaseFailed(PurchaseFailureReason reason)
		{
			_blocker.style.display = DisplayStyle.None;
			if (reason is PurchaseFailureReason.UserCancelled or PurchaseFailureReason.PaymentDeclined) return;


			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.UITShared.ok,
				ButtonOnClick = () => _gameServices.GenericDialogService.CloseDialog()
			};

			_gameServices.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error,
				string.Format(ScriptLocalization.UITStore.iap_error, reason.ToString()), false, confirmButton);
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

		private void OnItemRewarded(ItemRewardedMessage msg)
		{
			// Cores are handled above separately
			FLog.Verbose("Store Screen", $"Viewing Reward {msg.Item}");
			if (!msg.Item.Id.IsInGroup(GameIdGroup.Currency) && !msg.Item.Id.IsInGroup(GameIdGroup.Collection)) return;
			_gameServices.UIService.OpenScreen<RewardsScreenPresenter>(
				new RewardsScreenPresenter.StateData()
				{
					Items = new List<ItemData> {msg.Item},
					FameRewards = false,
					OnFinish = () =>
					{
						_gameServices.UIService.OpenScreen<StoreScreenPresenter>(Data).Forget();
					}
				}).Forget();
		}

		private void BuyItem(GameProduct product)
		{
			if (_blocker.style.display == DisplayStyle.Flex) return;

			_blocker.style.display = DisplayStyle.Flex;
			Data.OnPurchaseItem(product);
		}
	}
}