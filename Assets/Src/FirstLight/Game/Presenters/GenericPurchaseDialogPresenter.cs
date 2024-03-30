using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters.Store;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UiService;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles purchase confirmations
	/// </summary>
	[UILayer(UIService.UIService.UILayer.Popup)]
	public class GenericPurchaseDialogPresenter : UIPresenterData<GenericPurchaseDialogPresenter.StateData>
	{
		
		public class StateData
		{
			public GameId Currency = GameId.BlastBuck;
			public uint Value;
			public ulong OwnedCurrency;
			public ItemData Item;
			public string OverwriteTitle;
			public string OverwriteItemName;
			public Action OnConfirm;
			public Action OnExit;
			public Sprite ItemSprite;
		}
		
		private IGameServices _services;
		
		private Label _itemPrice;
		private Label _itemAmount;
		private Label _title;
		private Label _itemDisplayName;
		private VisualElement _notEnoughContainer;
		private Label _notEnoughText;
		private VisualElement _notEnoughIcon;
		private VisualElement _itemIcon;
		private Button _blockerButton;
		private ImageButton _closeButton;
		private ImageButton _buyButton;
		private VisualElement _costIcon;


		private Action _closeCallback;
		private Action _confirmCallback;

		private void Awake()
		{
			_services = MainInstaller.ResolveServices();
		}

		protected override UniTask OnScreenClose()
		{
			_closeCallback?.Invoke();
			return base.OnScreenClose();
		}

		protected override void QueryElements()
		{
			_itemDisplayName = Root.Q<Label>("Desc").Required();
			_title = Root.Q<Label>("Title").Required();
			_itemAmount = Root.Q<Label>("ItemAmount").Required();
			_itemIcon = Root.Q<VisualElement>("ItemIcon").Required();
			_itemPrice = Root.Q<Label>("ItemPrice").Required();
			_costIcon = Root.Q("CostIcon").Required();

			_buyButton = Root.Q<ImageButton>("BuyButton").Required();
			_closeButton = Root.Q<ImageButton>("CloseButton").Required();
			_blockerButton = Root.Q<Button>("BlockerButton").Required();

			_notEnoughIcon = Root.Q<VisualElement>("NotEnoughDescIcon").Required();
			_notEnoughText = Root.Q<Label>("NotEnoughDescLabel").Required();
			_notEnoughContainer = Root.Q<VisualElement>("NotEnoughDescContainer").Required();


			_confirmCallback = null;
			_closeCallback = null;

			_buyButton.clicked += OnBuyButtonClicked;
			_blockerButton.clicked += CloseRequested;
			_closeButton.clicked += CloseRequested;

			FLog.Verbose("Generic Purchase Dialog", "Opened and registered callbacks");
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			var notEnough = Data.OwnedCurrency < Data.Value;
			_title.text = ScriptLocalization.UITGeneric.purchase_title;
			if (!string.IsNullOrEmpty(Data.OverwriteTitle))
			{
				_title.text = Data.OverwriteTitle;
			}

			// amnt 0 to always show default currency icon
			var costIcon = ItemFactory.Currency(Data.Currency, 0);
			costIcon.GetViewModel().DrawIcon(_costIcon);

			_itemAmount.text = "";

			if (!string.IsNullOrEmpty(Data.OverwriteItemName))
			{
				_itemDisplayName.text = Data.OverwriteItemName;
			}
			else if (Data.Item != null)
			{
				var itemView = Data.Item.GetViewModel();
				_itemDisplayName.text = ScriptLocalization.UITGeneric.purchase_about_to_buy + " ";
				if (itemView.Amount > 1)
				{
					_itemDisplayName.text += itemView.Amount + " ";
					_itemAmount.text = $"{itemView.Amount}";
				}

				_itemDisplayName.text += itemView.DisplayName;
			}

			if (Data.ItemSprite != null)
			{
				_itemIcon.style.backgroundImage = new StyleBackground(Data.ItemSprite);
			}
			else
			{
				var itemView = Data.Item.GetViewModel();
				_itemIcon.style.backgroundImage = StyleKeyword.Null;
				itemView.DrawIcon(_itemIcon);
			}

			_itemPrice.text = Data.Value.ToString();

			_closeCallback = Data.OnExit;
			_confirmCallback = Data.OnConfirm;

			_notEnoughContainer.SetDisplay(notEnough);
			_costIcon.SetDisplay(!notEnough);
			if (!notEnough)
			{
				return base.OnScreenOpen(reload);
			}

			costIcon.GetViewModel().DrawIcon(_notEnoughIcon);
			var missing = Data.Value - Data.OwnedCurrency;
			_notEnoughText.text = string.Format(ScriptLocalization.UITGeneric.purchase_you_need_currency, missing, Data.Currency.GetCurrencyLocalization(missing).ToUpperInvariant());
			_itemPrice.text = string.Format(ScriptLocalization.UITGeneric.purchase_get_currency, Data.Currency.GetCurrencyLocalization(2).ToUpperInvariant());
			_closeCallback = Data.OnExit;
			_confirmCallback = GoToShop;
			
			return base.OnScreenOpen(reload);
		}

		private void CloseRequested()
		{
			_services.UIService.CloseScreen<GenericPurchaseDialogPresenter>();
		}

		private void OnBuyButtonClicked()
		{
			FLog.Verbose("Generic Purchase Dialog", "Buy Clicked");
			_confirmCallback.Invoke();
			CloseRequested();
		}

		private void GoToShop()
		{
			FLog.Verbose("Generic Purchase Dialog", "Go To Shop");
			if (!_services.UIService.IsScreenOpen<StoreScreenPresenter>())
			{
				MainInstaller.ResolveServices().IAPService.RequiredToViewStore = true;
			}
			else
			{
				// TODO: This seems hacky
				_services.UIService.GetScreen<StoreScreenPresenter>().GoToCategoryWithProduct(Data.Currency);
			}

			CloseRequested();
		}
	}
}