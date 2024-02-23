using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters.Store;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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
	public class GenericPurchaseDialogPresenter : UiToolkitPresenter
	{
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


		private GenericPurchaseOptions _options;
		private Action _closeCallback;
		private Action _confirmCallback;

		private void CloseRequested()
		{
			Close(false);
		}

		protected override UniTask OnClosed()
		{
			_closeCallback?.Invoke();
			return base.OnClosed();
		}

		protected override void QueryElements(VisualElement root)
		{
			_itemDisplayName = root.Q<Label>("Desc").Required();
			_title = root.Q<Label>("Title").Required();
			_itemAmount = root.Q<Label>("ItemAmount").Required();
			_itemIcon = root.Q<VisualElement>("ItemIcon").Required();
			_itemPrice = root.Q<Label>("ItemPrice").Required();
			_costIcon = root.Q("CostIcon").Required();

			_buyButton = root.Q<ImageButton>("BuyButton").Required();
			_closeButton = root.Q<ImageButton>("CloseButton").Required();
			_blockerButton = root.Q<Button>("BlockerButton").Required();

			_notEnoughIcon = root.Q<VisualElement>("NotEnoughDescIcon").Required();
			_notEnoughText = root.Q<Label>("NotEnoughDescLabel").Required();
			_notEnoughContainer = root.Q<VisualElement>("NotEnoughDescContainer").Required();


			_confirmCallback = null;
			_closeCallback = null;

			_buyButton.clicked += OnBuyButtonClicked;
			_blockerButton.clicked += CloseRequested;
			_closeButton.clicked += CloseRequested;

			FLog.Verbose("Generic Purchase Dialog", "Opened and registered callbacks");
			base.QueryElements(root);
		}

		private void OnBuyButtonClicked()
		{
			FLog.Verbose("Generic Purchase Dialog", "Buy Clicked");
			_confirmCallback.Invoke();
			CloseRequested();
		}

		public void SetOptions(GenericPurchaseOptions options)
		{
			_options = options;
			var notEnough = options.OwnedCurrency < options.Value;
			_title.text = ScriptLocalization.UITGeneric.purchase_title;
			if (!string.IsNullOrEmpty(options.OverwriteTitle))
			{
				_title.text = options.OverwriteTitle;
			}

			// amnt 0 to always show default currency icon
			var costIcon = ItemFactory.Currency(options.Currency, 0);
			costIcon.GetViewModel().DrawIcon(_costIcon);

			_itemAmount.text = "";

			if (!string.IsNullOrEmpty(options.OverwriteItemName))
			{
				_itemDisplayName.text = options.OverwriteItemName;
			}
			else if (options.Item != null)
			{
				var itemView = options.Item.GetViewModel();
				_itemDisplayName.text = ScriptLocalization.UITGeneric.purchase_about_to_buy + " ";
				if (itemView.Amount > 1)
				{
					_itemDisplayName.text += itemView.Amount + " ";
					_itemAmount.text = $"{itemView.Amount}";
				}

				_itemDisplayName.text += itemView.DisplayName;
			}

			if (options.ItemSprite != null)
			{
				_itemIcon.style.backgroundImage = new StyleBackground(options.ItemSprite);
			}
			else
			{
				var itemView = options.Item.GetViewModel();
				_itemIcon.style.backgroundImage = StyleKeyword.Null;
				itemView.DrawIcon(_itemIcon);
			}

			_itemPrice.text = options.Value.ToString();

			_closeCallback = options.OnExit;
			_confirmCallback = options.OnConfirm;

			_notEnoughContainer.SetDisplay(notEnough);
			_costIcon.SetDisplay(!notEnough);
			if (!notEnough)
			{
				return;
			}

			costIcon.GetViewModel().DrawIcon(_notEnoughIcon);
			var missing = options.Value - options.OwnedCurrency;
			_notEnoughText.text = string.Format(ScriptLocalization.UITGeneric.purchase_you_need_currency, missing, options.Currency.GetCurrencyLocalization(missing).ToUpperInvariant());
			_itemPrice.text = string.Format(ScriptLocalization.UITGeneric.purchase_get_currency, options.Currency.GetCurrencyLocalization(2).ToUpperInvariant());
			_closeCallback = options.OnExit;
			_confirmCallback = GoToShop;
		}


		private void GoToShop()
		{
			FLog.Verbose("Generic Purchase Dialog", "Go To Shop");
			if (_uiService.GetCurrentOpenedScreen().GetType() != typeof(StoreScreenPresenter))
			{
				MainInstaller.ResolveServices().IAPService.RequiredToViewStore = true;
			}
			else
			{
				((StoreScreenPresenter) _uiService.GetCurrentOpenedScreen()).GoToCategoryWithProduct(_options.Currency);
			}

			CloseRequested();
		}

		public class GenericPurchaseOptions
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
	}
}