using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters.Store
{
	public class StoreGameProductElement : VisualElement
	{
		public const string UssSmall = "product-widget--small";

		public const string UssProductName = "product-name";
		public const string UssProductImage = "product-image";
		public const string UssProductPrice = "product-price";
		public const string UssProductWidget = "product-widget";
		public const string UssBundleImage = "product-background-image";
		public const string UssGradientSides = "product-image-gradient-sides";
		public const string UssGradientBig = "product-image-gradient-big";
		public const string UssGradientSmall = "product-image-gradient-small";
		public const string UssWidgetEffects = "widget-effect";
		
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
			UssBundleImage, UssGradientSides, UssGradientBig, 
			UssGradientSmall, UssWidgetEffects
		};

		private string[] _sizeable =
		{
			UssBundleImage, UssGradientSides, UssGradientBig, 
			UssGradientSmall, UssWidgetEffects, UssProductWidget,
			UssProductPrice, UssProductImage, UssProductName
		};

		private Label _name;
		private VisualElement _icon;
		private Button _root;
		private Label _price;

		public StoreGameProductElement()
		{
			var treeAsset = Resources.Load<VisualTreeAsset>("StoreGameProductElement");
			treeAsset.CloneTree(this);
			_root = this.Q<Button>("ProductWidget").Required();
			_root.clicked += () => OnClicked(_product);
			_name = this.Q<Label>("ProductName").Required();
			_icon = this.Q("ProductImage").Required();
			_price = this.Q<Label>("ProductPrice").Required();
		}

		public void SetData(GameProduct product)
		{
			_product = product;
			var itemView = product.GameItem.GetViewModel();
			_name.text = "";
			if (itemView.Amount > 1) _name.text += itemView.Amount + " ";
			_name.text += itemView.DisplayName;
			_price.text = (product.PlayfabProductConfig.StoreItem.VirtualCurrencyPrices["RM"] / 100d).ToString();
			itemView.DrawIcon(_icon);
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
				final += Char.ToUpper(piece[0]) + piece.Substring(1).ToLower();
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
				});
			}
		}
	}
}