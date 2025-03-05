using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Presenters.Store;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.UIElements.Kit;
using FirstLight.Game.Utils;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles purchase confirmations
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class GenericPurchaseDialogPresenter : UIPresenterData<GenericPurchaseDialogPresenter.StateData>
	{
		public class StateData
		{
			public IPurchaseData PurchaseData;
			public ulong OwnedCurrency;
		}

		public interface IPurchaseData
		{
			public string Title { get; }
			public ItemData Price { get; }

			/// <summary>
			/// Called everytime the screen is closed
			/// The boolean is true if player is required to see the shop
			/// </summary>
			public Action<bool> OnExit { get; }

			/// <summary>
			/// Called when the user confirm the transaction and has the currency
			/// </summary>
			public Action OnConfirm { get; }
		}

		public class TextPurchaseData : IPurchaseData
		{
			public string Title { get; set; }
			public ItemData Price { get; set; }
			public Action<bool> OnExit { get; set; }
			public Action OnConfirm { get; set; }

			public Action OnGoToShopRequired { get; set; }
			public string TextFormat { get; set; }
		}

		public class IconPurchaseData : IPurchaseData
		{
			public GameId Currency = GameId.BlastBuck;
			public uint Value;
			public ItemData Item;
			public string OverwriteTitle;
			public string OverwriteItemName;
			public Action OnConfirm;
			public Action<bool> OnExit;
			public Sprite ItemSprite;
			public string Title => OverwriteTitle;
			public ItemData Price => ItemFactory.Currency(Currency, (int) Value);

			Action<bool> IPurchaseData.OnExit => OnExit;

			Action IPurchaseData.OnConfirm => OnConfirm;

		}

		public class TextPurchaseView : UIView
		{
			[Q("ConfirmButton")] private KitButton _confirmButton;
			[Q("TextContent")] private Label _textContent;
			[Q("Controls")] private VisualElement _controls;
			public Action OnClickConfirm;

			public void Setup(TextPurchaseData data, bool hasCurrency, ulong ownedCurrency, bool canBeBought)
			{
				_confirmButton.clicked += OnClickConfirm;

				var sprite = CurrencyItemViewModel.GetRichTextIcon(data.Price.Id);
				if (hasCurrency)
				{
					var price = data.Price.GetMetadata<CurrencyMetadata>().Amount;
					_textContent.text = string.Format(data.TextFormat, $"{price} {sprite}");
				}
				else if (canBeBought)
				{
					_textContent.text = "Visit the shop to get some more " + sprite;
					_confirmButton.BtnText = ScriptLocalization.UITGeneric.purchase_not_enough_button_text;
				}
				else
				{
					_textContent.text = "You don't have enough " + sprite;
					_confirmButton.BtnText = ScriptLocalization.General.OK;
				}
			}

			public TextPurchaseView SetupSimpleText(string text)
			{
				_textContent.text = text;
				return this;
			}

			public TextPurchaseView ClearButtons()
			{
				_controls.Clear();
				return this;
			}

			public TextPurchaseView AddButton(string text, ButtonColor color, Action action)
			{
				var btn = new KitButton()
				{
					BtnColor = color,
					BtnText = text,
					BtnShape = ButtonShape.Long,
					BtnStyle = ButtonStyle.Solid,
				};
				if (_controls.childCount > 0)
				{
					btn.AddDefaultGap();
				}

				btn.clicked += action;
				_controls.Add(btn);
				return this;
			}
		}

		public class IconPurchaseView : UIView
		{
			[Q("ItemPrice")] private Label _itemPrice;
			[Q("ItemAmount")] private Label _itemAmount;
			[Q("Desc")] private Label _itemDisplayName;
			[Q("NotEnoughDescContainer")] private VisualElement _notEnoughContainer;
			[Q("NotEnoughDescIcon")] private VisualElement _notEnoughIcon;
			[Q("NotEnoughDescLabel")] private Label _notEnoughText;
			[Q("ItemIcon")] private VisualElement _itemIcon;
			[Q("BuyButton")] private ImageButton _buyButton;
			[Q("CostIcon")] private VisualElement _costIcon;
			public Action OnClickConfirm;

			public void Setup(IconPurchaseData data, bool hasCurrency, ulong ownedCurrency, bool canBeBought)
			{
				_buyButton.clicked += OnClickConfirm;
				// amnt 0 to always show default currency icon
				var costIcon = ItemFactory.Currency(data.Currency, 0);
				costIcon.GetViewModel().DrawIcon(_costIcon);

				_itemAmount.text = "";

				if (!string.IsNullOrEmpty(data.OverwriteItemName))
				{
					_itemDisplayName.text = data.OverwriteItemName;
				}
				else if (data.Item != null)
				{
					var itemView = data.Item.GetViewModel();
					_itemDisplayName.text = ScriptLocalization.UITGeneric.purchase_about_to_buy + " ";
					if (itemView.Amount > 1)
					{
						_itemDisplayName.text += itemView.Amount + " ";
						_itemAmount.text = $"{itemView.Amount}";
					}

					_itemDisplayName.text += itemView.DisplayName;
				}

				if (data.ItemSprite != null)
				{
					_itemIcon.style.backgroundImage = new StyleBackground(data.ItemSprite);
				}
				else
				{
					var itemView = data.Item.GetViewModel();
					_itemIcon.style.backgroundImage = StyleKeyword.Null;
					itemView.DrawIcon(_itemIcon);
				}

				_itemPrice.text = data.Value.ToString();
				_notEnoughContainer.SetDisplay(!hasCurrency);
				_costIcon.SetDisplay(hasCurrency);
				if (hasCurrency)
				{
					return;
				}

				costIcon.GetViewModel().DrawIcon(_notEnoughIcon);
				var missing = data.Value - ownedCurrency;
				_notEnoughText.text = string.Format(ScriptLocalization.UITGeneric.purchase_you_need_currency, missing,
					data.Currency.GetCurrencyLocalization(missing).ToUpperInvariant());

				_itemPrice.text = !canBeBought ? ScriptLocalization.General.OK : ScriptLocalization.UITGeneric.purchase_not_enough_button_text;
			}
		}

		private IGameServices _services;

		[Q("Popup")] private GenericPopupElement _popup;
		[QView("IconSetup")] private IconPurchaseView _iconPurchaseView;
		[QView("TextSetup")] private TextPurchaseView _textPurchaseView;
		[Q("BlockerButton")] private Button _blockerButton;

		private Action _confirmCallback;
		private bool _forcePlayerToShop;
		private bool _bought;

		private void Awake()
		{
			_services = MainInstaller.ResolveServices();
		}

		protected override UniTask OnScreenClose()
		{
			if (!_bought)
			{
				Data.PurchaseData.OnExit?.Invoke(_forcePlayerToShop);
			}

			return base.OnScreenClose();
		}

		protected override void QueryElements()
		{
			_iconPurchaseView.OnClickConfirm += OnBuyButtonClicked;
			_textPurchaseView.OnClickConfirm += OnBuyButtonClicked;

			_popup.CloseClicked += CloseRequested;
			_blockerButton.clicked += CloseRequested;

			_confirmCallback = null;

			FLog.Verbose("Generic Purchase Dialog", "Opened and registered callbacks");
		}

		public bool CanBeBoughtOnShop(GameId gameId)
		{
			return _services.IAPService.AvailableProductCategories.SelectMany(ctg => ctg.Products)
				.Any(product => gameId == product.GameItem.Id);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			FLog.Verbose("Opening Generic Purchase Dialog");
			_forcePlayerToShop = false;
			_bought = false;
			var purchaseData = Data.PurchaseData;
			var hasCurrency = Data.OwnedCurrency >= (ulong) purchaseData.Price.GetMetadata<CurrencyMetadata>().Amount;
			var canBeBought = CanBeBoughtOnShop(Data.PurchaseData.Price.Id);
			_confirmCallback = !hasCurrency ? canBeBought ? GoToShop : () => { } : purchaseData.OnConfirm;

			if (hasCurrency)
			{
				_popup.SetTitle(!string.IsNullOrEmpty(purchaseData.Title) ? purchaseData.Title : ScriptLocalization.UITGeneric.purchase_title);
			}
			else
			{
				_popup.SetTitle(string.Format(ScriptLocalization.UITGeneric.purchase_not_enough_title,
					purchaseData.Price.Id.GetCurrencyLocalization(2).ToUpperInvariant()));
			}

			if (!hasCurrency && purchaseData.Price.Id == GameId.NOOB)
			{
				_iconPurchaseView.Element.SetDisplay(false);
				_textPurchaseView.Element.SetDisplay(true);
				_textPurchaseView
					.ClearButtons()
					.SetupSimpleText(ScriptLocalization.UITGeneric.purchase_not_enough_noob_text)
					.AddButton(ScriptLocalization.UITGeneric.purchase_not_enough_noob_discord_button, ButtonColor.Primary, () =>
					{
						Application.OpenURL(GameConstants.Links.DISCORD_SERVER);
						CloseRequested();
					})
					.AddButton(ScriptLocalization.UITGeneric.purchase_not_enough_noob_guide_button, ButtonColor.Success, () =>
					{
						Application.OpenURL(GameConstants.Links.NOOB_GUIDE);
						CloseRequested();
					});
			}
			else if (purchaseData is IconPurchaseData iconData)
			{
				_textPurchaseView.Element.SetDisplay(false);
				_iconPurchaseView.Element.SetDisplay(true);
				_iconPurchaseView.Setup(iconData, hasCurrency, Data.OwnedCurrency, canBeBought);
			}
			else if (purchaseData is TextPurchaseData textData)
			{
				_textPurchaseView.Element.SetDisplay(true);
				_iconPurchaseView.Element.SetDisplay(false);
				_textPurchaseView.Setup(textData, hasCurrency, Data.OwnedCurrency, canBeBought);
			}

			return UniTask.CompletedTask;
		}

		private void CloseRequested()
		{
			_services.UIService.CloseScreen<GenericPurchaseDialogPresenter>();
		}

		private void OnBuyButtonClicked()
		{
			FLog.Verbose("Generic Purchase Dialog", "Buy Clicked");
			_confirmCallback.Invoke();
			_bought = true;
			CloseRequested();
		}

		private void GoToShop()
		{
			_forcePlayerToShop = true;
			FLog.Verbose("Generic Purchase Dialog", "Go To Shop");
			if (!_services.UIService.IsScreenOpen<StoreScreenPresenter>())
			{
				_services.HomeScreenService.SetForceBehaviour(HomeScreenForceBehaviourType.Store);
			}
			else
			{
				// TODO: This seems hacky
				_services.UIService.GetScreen<StoreScreenPresenter>().GoToCategoryWithProduct(Data.PurchaseData.Price.Id);
			}
		}
	}
}