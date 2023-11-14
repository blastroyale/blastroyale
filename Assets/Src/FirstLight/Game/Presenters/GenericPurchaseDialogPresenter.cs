using System;
using System.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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

		public void SetOptions(GenericPurchaseOptions options)
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
				options.Item.GetViewModel().DrawIcon(_itemIcon);

			}

			_itemPrice.text = options.Value.ToString();
			_confirmCallback = options.OnConfirm;
			_closeCallback = options.OnExit;
			_buyButton.clicked += OnBuyButtonClicked;
			_buyButton.clicked += CloseRequested;
			_blockerButton.clicked += CloseRequested;
			_closeButton.clicked += CloseRequested;
		}
		

		public void SetNotEnoughBlastBucks()
		{
			Root.AddToClassList(USS_NOT_ENOUGH_FUNDS);
			_itemDisplayName.text = "VISIT THE SHOP TO GET SOME MORE";
			_itemPrice.text = "GO TO SHOP";
			var itemIcon = ItemFactory.Currency(GameId.BlastBuck, 1);
			itemIcon.GetViewModel().DrawIcon(_itemIcon);

			_itemPrice.text = "GO TO SHOP";
			_title.text = "NOT ENOUGH BLAST BUCKS";
			_buyButton.clicked += GoToShop;
			_buyButton.clicked += CloseRequested;
			_blockerButton.clicked += CloseRequested;
			_closeButton.clicked += CloseRequested;
		}

		private void GoToShop()
		{
		}

		public class GenericPurchaseOptions
		{
			public ulong Value;
			public ItemData Item;
			public string OverwriteTitle;
			public string OverwriteItemName;
			public Action OnConfirm;
			public Action OnExit;
			public Sprite ItemSprite;
		}
	}
}