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
		private const string USS_NOT_ENOUGH_FUNDS = "purchase-confirmation-root--insufficient";
		private Label _itemPrice;
		private Label _itemAmount;
		private Label _title;
		private Label _itemDisplayName;
		private VisualElement _itemIcon;
		private Button _blockerButton;
		private ImageButton _closeButton;
		private ImageButton _buyButton;
		private VisualElement _costIcon;


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

			_confirmCallback = null;
			_closeCallback = null;

			_buyButton.clicked += OnBuyButtonClicked;
			_blockerButton.clicked += CloseRequested;
			_closeButton.clicked += CloseRequested;
			
			FLog.Verbose("Generic Purchase Dialog","Opened and registered callbacks");
			base.QueryElements(root);
		}

		private void OnBuyButtonClicked()
		{
			FLog.Verbose("Generic Purchase Dialog","Buy Clicked");
			_confirmCallback.Invoke();
		}

		public void SetHasEnoughOptions(GenericPurchaseOptions options)
		{
			Root.RemoveModifiers();
			if (!string.IsNullOrEmpty(options.OverwriteTitle))
			{
				_title.text = options.OverwriteTitle;
			}
			
			var costIcon = ItemFactory.Currency(options.Currency, (int)options.Value);
			costIcon.GetViewModel().DrawIcon(_costIcon);

			if (!string.IsNullOrEmpty(options.OverwriteItemName))
			{
				_itemDisplayName.text = options.OverwriteItemName;
			}
			else if (options.Item != null)
			{
				_itemDisplayName.text = options.Item.GetDisplayName();
			}

			if (options.ItemSprite != null)
			{
				_itemIcon.style.backgroundImage = new StyleBackground(options.ItemSprite);
			}
			else
			{
				_itemIcon.style.backgroundImage = StyleKeyword.Null;
				options.Item.GetViewModel().DrawIcon(_itemIcon);
			}

			_itemAmount.text = "";
			_itemPrice.text = options.Value.ToString();

			_title.text = ScriptLocalization.UITGeneric.purchase_title;
			_closeCallback = options.OnExit;
			_confirmCallback = options.OnConfirm;
		}


		public void SetNotEnoughOptions(GenericPurchaseOptions options)
		{
			_itemIcon.style.backgroundImage = StyleKeyword.Null;
			Root.AddToClassList(USS_NOT_ENOUGH_FUNDS);
		
			var costIcon = ItemFactory.Currency(options.Currency, (int)options.Value);
			costIcon.GetViewModel().DrawIcon(_itemIcon);

			_title.text = $"{ScriptLocalization.UITGeneric.purchase_not_enough_title} {costIcon.GetDisplayName().ToUpper()}S";;
			_itemAmount.text = options.Value > 0 ? options.Value.ToString() : "";
			_closeCallback = options.OnExit;
			_confirmCallback = GoToShop;
			if (_uiService.GetCurrentOpenedScreen().GetType() == typeof(StoreScreenPresenter))
			{
				_itemPrice.text = ScriptLocalization.General.OK;
				_itemDisplayName.text = "";
			}
			else
			{
				_itemDisplayName.text = ScriptLocalization.UITGeneric.purchase_not_enough_title;
				_itemPrice.text = ScriptLocalization.UITGeneric.purchase_not_enough_button_text;
			}
		}

		private void GoToShop()
		{
			FLog.Verbose("Generic Purchase Dialog","Go To Shop");
			if (_uiService.GetCurrentOpenedScreen().GetType() != typeof(StoreScreenPresenter))
			{
				MainInstaller.ResolveServices().IAPService.RequiredToViewStore = true;
			}
			CloseRequested();
		}

		public class GenericPurchaseOptions
		{
			public GameId Currency = GameId.BlastBuck;
			public uint Value;
			public ItemData Item;
			public string OverwriteTitle;
			public string OverwriteItemName;
			public Action OnConfirm;
			public Action OnExit;
			public Sprite ItemSprite;
		}
	}
}
