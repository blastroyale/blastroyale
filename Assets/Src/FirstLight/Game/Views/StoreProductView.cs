using System;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Views
{
	public class StoreProductView : UIView
	{
		public const string UssSmall = "product-widget--small";
		
		public Action<GameProduct> OnClicked;

		private GameProduct _product;
		private Label _name;
		private VisualElement _icon;
		private Button _root;
		private Label _price;
		
		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_root = element.Q<Button>("ProductWidget").Required();
			_root.clicked += () => OnClicked(_product);
			_name = element.Q<Label>("ProductName").Required();
			_icon = element.Q("ProductImage").Required();
			_price = element.Q<Label>("ProductPrice").Required();
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

		private void FormatByStoreData()
		{
			var storeData = _product.PlayfabProductConfig.StoreItemData;
			if (storeData.Size == StoreDisplaySize.Full) _root.RemoveFromClassList(UssSmall);
			else _root.AddToClassList(UssSmall);

			if (!string.IsNullOrEmpty(storeData.BackgroundColor))
			{
				ColorUtility.TryParseHtmlString(storeData.BackgroundColor, out Color color);
				_root.style.unityBackgroundImageTintColor = color;
			}

			if (!string.IsNullOrEmpty(storeData.ImageOverride))
			{
				_icon.ClearClassList();
				MainInstaller.ResolveServices().RemoteTextureService.RequestTexture(storeData.ImageOverride, texture2D =>
				{
					_icon.style.backgroundImage = new StyleBackground(texture2D);
				});
			}
		}
	}
}