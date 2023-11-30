using System;
using System.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
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


		private Action _closeCallback;
		private Action _confirmCallback;

		private void CloseRequested()
		{
			Close(false);
		}

		protected override void Close(bool destroy)
		{
			// TODO - check if IsOpenedComplete check needs to be added to prevent "closing too early" edge cases
			base.Close(destroy);
			_blockerButton.clicked -= CloseRequested;
			_buyButton.clicked -= CloseRequested;
			_buyButton.clicked -= OnBuyButtonClicked;
		}

		protected override Task OnClosed()
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

			_buyButton = root.Q<ImageButton>("BuyButton").Required();
			_closeButton = root.Q<ImageButton>("CloseButton").Required();
			_blockerButton = root.Q<Button>("BlockerButton").Required();

			_confirmCallback = null;
			_closeCallback = null;
		}

		private void OnBuyButtonClicked()
		{
			_confirmCallback.Invoke();
		}

		public void SetHasEnoughOptions(GenericPurchaseOptions options)
		{
			Root.RemoveModifiers();
			if (!string.IsNullOrEmpty(options.OverwriteTitle))
			{
				_title.text = options.OverwriteTitle;
			}

			if (!string.IsNullOrEmpty(options.OverwriteItemName))
			{
				_itemDisplayName.text = options.OverwriteItemName;
			}
			else if(options.Item != null)
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
			_confirmCallback = options.OnConfirm;
			_title.text = ScriptLocalization.UITGeneric.purchase_title;
			_closeCallback = options.OnExit;
			_buyButton.clicked += OnBuyButtonClicked;
			_blockerButton.clicked += CloseRequested;
			_closeButton.clicked += CloseRequested;
		}
		

		public void SetNotEnoughOptions(GenericPurchaseOptions options)
		{
			_itemIcon.style.backgroundImage = StyleKeyword.Null;
			Root.AddToClassList(USS_NOT_ENOUGH_FUNDS);
			_itemDisplayName.text = ScriptLocalization.UITGeneric.purchase_not_enough_item_display_name;
			var itemIcon = ItemFactory.Currency(GameId.BlastBuck, 1);
			itemIcon.GetViewModel().DrawIcon(_itemIcon);

			_itemPrice.text = ScriptLocalization.UITGeneric.purchase_not_enough_button_text;
			_title.text = ScriptLocalization.UITGeneric.purchase_not_enough_title;
			_itemAmount.text = options.Value > 0 ? options.Value.ToString() : "";
	
			_buyButton.clicked += GoToShop;
			_blockerButton.clicked += CloseRequested;
			_closeButton.clicked += CloseRequested;
			_closeCallback = options.OnExit;
		}

		private void GoToShop()
		{
			MainInstaller.ResolveServices().IAPService.RequiredToViewStore = true;
			CloseRequested();
		}

		public class GenericPurchaseOptions
		{
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