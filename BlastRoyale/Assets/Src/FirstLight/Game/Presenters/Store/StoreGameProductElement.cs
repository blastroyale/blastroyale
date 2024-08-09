using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Models;
using I2.Loc;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.Store
{
	[Flags]
	public enum ProductFlags : byte
	{
		NONE = 0,
		OWNED = 1 << 1,
		NEW = 1 << 2
	}

	public class StoreGameProductElement : VisualElement
	{
		private const string USS_PRODUCT_NAME = "product-name";
		private const string USS_PRODUCT_IMAGE = "product-image";
		private const string USS_PRODUCT_PRICE = "product-price";
		private const string USS_PRODUCT_WIDGET = "product-widget";
		private const string USS_INFO = "info-button";
		private const string USS_BUNDLE_IMAGE = "product-background-image";
		private const string USS_GRADIENT_SIDES = "product-image-gradient-sides";
		private const string USS_GRADIENT_BIG = "product-image-gradient-big";
		private const string USS_GRADIENT_SMALL = "product-image-gradient-small";
		private const string USS_WIDGET_EFFECTS = "widget-effect";
		private const string USS_OWNED_STAMP = "owned-stamp";
		private const string USS_OWNED_STAMP_TEXT = "owned-stamp-text";
		private const string USS_OWNED_MODIFIER = "--owned";
		private const string USS_SPRITE_CURRENCIES_BLASTBUCK = "sprite-currencies__blastbuck-1";

		public StoreDisplaySize size { get; set; }
		public GameId gameId { get; set; }
		public string imageOverwrite { get; set; }

		public Action<GameProduct> OnClicked;
		private GameProduct _product;

		/// <summary>
		/// Elements that can receive generic modifiers
		/// </summary>
		private string[] _modifiable =
		{
			USS_BUNDLE_IMAGE, USS_GRADIENT_SIDES, USS_GRADIENT_BIG,
			USS_GRADIENT_SMALL, USS_WIDGET_EFFECTS
		};

		private string[] _sizeable =
		{
			USS_BUNDLE_IMAGE, USS_GRADIENT_SIDES, USS_GRADIENT_BIG,
			USS_GRADIENT_SMALL, USS_WIDGET_EFFECTS, USS_PRODUCT_WIDGET,
			USS_PRODUCT_PRICE, USS_PRODUCT_IMAGE, USS_PRODUCT_NAME, USS_OWNED_STAMP, USS_OWNED_STAMP_TEXT
		};

		private Label _name;
		private VisualElement _icon;
		private VisualElement _root;
		private Label _price;
		private ImageButton _infoButton;
		private UnityEngine.UIElements.Button _background;
		private VisualElement _ownedStamp;
		private VisualElement _infoIcon;
		private VisualElement _ownedOverlay;

		public StoreGameProductElement()
		{
			var treeAsset = Resources.Load<VisualTreeAsset>("StoreGameProductElement");
			treeAsset.CloneTree(this);
			_background = this.Q<UnityEngine.UIElements.Button>("ProductWidgetWrapper").Required();
			_background.clicked += () => OnClicked?.Invoke(_product);
			_name = this.Q<Label>("ProductName").Required();
			_icon = this.Q("ProductImage").Required();
			_icon.AddToClassList(USS_SPRITE_CURRENCIES_BLASTBUCK);
			_price = this.Q<Label>("ProductPrice").Required();
			_ownedStamp = this.Q("OwnedStamp").Required();
			_ownedStamp.SetDisplay(false);
			_infoButton = this.Q<ImageButton>("InformationClickArea").Required();
			_infoIcon = this.Q("InformationIcon").Required();
			_ownedOverlay = this.Q("OwnedOverlay").Required();
			_infoButton.clicked += OnClickInfo;
		}

		private void OnClickInfo()
		{
			var desc = _product.PlayfabProductConfig.StoreItemData.Description;
			_infoButton.OpenTooltip(_root, desc);
		}

		public void SetData(GameProduct product, ProductFlags flags, VisualElement rootDocument)
		{
			_root = rootDocument;
			_product = product;
			var itemView = product.GameItem.GetViewModel();
			_name.text = "";
			if (itemView.Amount > 1) _name.text += itemView.Amount + " ";
			_name.text += itemView.DisplayName;

			if (flags.HasFlag(ProductFlags.OWNED))
			{
				_ownedOverlay.SetDisplay(true);
				_ownedStamp.SetDisplay(true);
				_icon.AddToClassList(USS_PRODUCT_NAME + USS_OWNED_MODIFIER);
				_name.AddToClassList(USS_PRODUCT_NAME + USS_OWNED_MODIFIER);
				_infoIcon.AddToClassList(USS_INFO + USS_OWNED_MODIFIER);
			}

			var price = product.GetPrice();
			FLog.Verbose("Store Screen", $"Setting up store item {product.GameItem}, price={price}");

			var priceConfig = product.PlayfabProductConfig.CatalogItem.VirtualCurrencyPrices.First();

			if (flags.HasFlag(ProductFlags.OWNED))
			{
				_price.text = "";
			}
			else if (price.item == GameId.RealMoney)
			{
				_price.text = product.UnityIapProduct().metadata.localizedPriceString;
			}
			else
			{
				var currencyItem = ItemFactory.Currency(price.item, 1);
				var currencyView = (CurrencyItemViewModel) currencyItem.GetViewModel();
				var priceIcon = currencyView.GetRichTextIcon();
				_price.text = priceIcon + " " + priceConfig.Value;
			}

			itemView.DrawIcon(_icon);

			_infoButton.SetDisplay(flags.HasFlag(ProductFlags.OWNED) || !string.IsNullOrEmpty(_product.PlayfabProductConfig.StoreItemData.Description));
			FormatByStoreData();
		}

		/// <summary>
		/// element-name to ElementName
		/// </summary>
		private string ClassNameToElementName(string s)
		{
			var final = "";
			foreach (var piece in s.Split("-"))
			{
				final += Char.ToUpperInvariant(piece[0]) + piece.Substring(1).ToLowerInvariant();
			}

			return final;
		}

		private void FormatByStoreData()
		{
			size = _product.PlayfabProductConfig.StoreItemData.Size;
			if (_product.PlayfabProductConfig.StoreItemData.Size == StoreDisplaySize.Half)
			{
				foreach (var sizeable in _sizeable)
				{
					var element = this.Q(ClassNameToElementName(sizeable));
					element.AddToClassList($"{sizeable}--small");
				}
			}

			var customModifier = _product.PlayfabProductConfig.StoreItemData.UssModifier;
			if (!string.IsNullOrEmpty(customModifier))
			{
				foreach (var modifiable in _modifiable)
				{
					var element = this.Q(ClassNameToElementName(modifiable));
					element.AddToClassList($"{modifiable}--{customModifier}");
				}
			}

			var imageOverride = _product.PlayfabProductConfig.StoreItemData.ImageOverride;
			if (!string.IsNullOrEmpty(imageOverride))
			{
				_icon.ClearClassList();
				MainInstaller.ResolveServices().RemoteTextureService.RequestTexture(imageOverride, texture2D =>
				{
					_icon.style.backgroundImage = new StyleBackground(texture2D);
				});
			}
		}

		public new class UxmlFactory : UxmlFactory<StoreGameProductElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlEnumAttributeDescription<StoreDisplaySize> _size = new ()
			{
				name = "small",
				defaultValue = StoreDisplaySize.Full,
			};

			private readonly UxmlEnumAttributeDescription<GameId> _gameId = new ()
			{
				name = "game-id",
				defaultValue = GameId.FemaleCorpos,
			};

			private readonly UxmlStringAttributeDescription _imageOverwrite = new ()
			{
				name = "image-overwrite",
				defaultValue = "",
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var ge = (StoreGameProductElement) ve;
				ge.SetData(new GameProduct
				{
					GameItem = ItemFactory.Legacy(new LegacyItemData()
					{
						RewardId = _gameId.GetValueFromBag(bag, cc),
						Value = 1,
					}),
					PlayfabProductConfig = new PlayfabProductConfig()
					{
						StoreItemData = new StoreItemData()
						{
							Size = _size.GetValueFromBag(bag, cc),
							ImageOverride = _imageOverwrite.GetValueFromBag(bag, cc)
						},
						StoreItem = new StoreItem()
						{
							VirtualCurrencyPrices = new Dictionary<string, uint>()
							{
								{"RM", 10000}
							}
						}
					},
				}, 0, ve);
			}
		}
	}
}