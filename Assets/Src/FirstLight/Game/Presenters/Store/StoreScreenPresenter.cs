using System;
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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
	[LoadSynchronously]
	public class StoreScreenPresenter : UiToolkitPresenterData<StoreScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<GameProduct> OnPurchaseItem;
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		public const string UssCategory = "product-category";
		public const string UssCategoryLabel = "category-label";
		public const string UssCategoryButton = "category-button";

		[SerializeField] private VisualTreeAsset _StoreProductView;
		
		private IGameServices _gameServices;

		private VisualElement _blocker;
		private ScreenHeaderElement _header;
		private VisualElement _productList;
		private VisualElement _categoryList;
		private ScrollView _scroll;

		private void Awake()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_blocker = root.Q("Blocker").Required();

			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_productList = root.Q("ProductList").Required();
			_categoryList = root.Q("Categories").Required();
			_scroll = root.Q<ScrollView>("ProductScrollView").Required();
			_header.backClicked += Data.OnBackClicked;
			_header.homeClicked += Data.OnHomeClicked;

			foreach (var category in _gameServices.IAPService.AvailableProductCategories)
			{
				var categoryElement = new StoreCategoryElement(category.Name);
				foreach (var product in category.Products)
				{
					var productElement = new StoreGameProductElement();
					productElement.SetData(product, root);
					productElement.OnClicked = BuyItem;
					categoryElement.Add(productElement);
					categoryElement.EnsureSize(productElement.size);
				}
				
				_productList.Add(categoryElement);

				var categoryButton = new Button();
				categoryButton.text = category.Name;
				categoryButton.AddToClassList(UssCategoryButton);
				categoryButton.clicked += () => SelectCategory(categoryElement, category);
				_categoryList.Add(categoryButton);
			}
			base.QueryElements(root);
		}

		private void SelectCategory(VisualElement categoryContainer, GameProductCategory category)
		{
			var targetX = categoryContainer.resolvedStyle.left;
			_scroll.experimental.animation.Start(0, 1f, 300, (element, percent) =>
			{
				var scrollView = (ScrollView) element;
				var currentScroll = scrollView.scrollOffset;
				scrollView.scrollOffset = new Vector2(targetX * percent, currentScroll.y);
			}).Ease(Easing.OutCubic);
		}

		protected override void SubscribeToEvents()
		{
			_gameServices.MessageBrokerService.Subscribe<OpenedCoreMessage>(OnCoresOpened);
			_gameServices.MessageBrokerService.Subscribe<ItemRewardedMessage>(OnItemRewarded);
			_gameServices.IAPService.UnityStore.OnPurchaseFailure += OnPurchaseFailed;
		}

		[Button]
		private void OnPurchaseFailed(PurchaseFailureReason reason)
		{
			_blocker.style.display = DisplayStyle.None;
			if (reason is PurchaseFailureReason.UserCancelled or PurchaseFailureReason.PaymentDeclined) return;

#if UNITY_EDITOR
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.UITShared.ok,
				ButtonOnClick = () => _gameServices.GenericDialogService.CloseDialog()
			};

			_gameServices.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, 
				string.Format(ScriptLocalization.UITStore.iap_error, reason.ToString()), false, confirmButton);
#else
			var button = new FirstLight.NativeUi.AlertButton
			{
				Style = FirstLight.NativeUi.AlertButtonStyle.Positive,
				Text = ScriptLocalization.UITShared.ok
			};

			FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.ErrorGeneric, reason.ToString(),
				button);
#endif
		}

		private void OnCoresOpened(OpenedCoreMessage msg)
		{
			_gameServices.GameUiService.OpenScreenAsync<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
			{
				ParentItem = msg.Core,
				Items = msg.Results,
				FameRewards = false,
				OnFinish = () =>
				{
					_gameServices.GameUiService.OpenScreenAsync<StoreScreenPresenter, StateData>(Data);
				}
			});
		}
		
		private void OnItemRewarded(ItemRewardedMessage msg)
		{
			// Handle only currency, other types are handled by claiming rewards
			if (!msg.Item.Id.IsInGroup(GameIdGroup.Currency)) return;
			_gameServices.GameUiService.OpenScreenAsync<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
			{
				Items = new List<ItemData> {msg.Item},
				FameRewards = false,
				OnFinish = () =>
				{
					_gameServices.GameUiService.OpenScreenAsync<StoreScreenPresenter, StateData>(Data);
				}
			});
		}

		protected override void UnsubscribeFromEvents()
		{
			_gameServices.MessageBrokerService.UnsubscribeAll(this);
			_gameServices.IAPService.UnityStore.OnPurchaseFailure -= OnPurchaseFailed;
		}

		private void BuyItem(GameProduct product)
		{
			if (_blocker.style.display == DisplayStyle.Flex) return;
			
			_blocker.style.display = DisplayStyle.Flex;
			Data.OnPurchaseItem(product);
		}
	}
}