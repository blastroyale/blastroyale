using System;
using FirstLight.Game.Infos;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays a category of items in the collection screen, e.g. Characters, Banners, Gliders, etc.
	/// </summary>
	public class CollectionCategoryElement : ImageButton
	{
		private const string UssBlock = "collection-category";
		private const string UssBlockSelected = UssBlock + "--selected";
		private const string UssBlockHighlighted = UssBlock + "--highlighted";

		private const string UssSelected = UssBlock + "__selected-bg";
		private const string UssHighlight = UssBlock + "__highlight";
		private const string UssBackground = UssBlock + "__background";
		private const string UssCardHolder = UssBlock + "__card-holder";

		private const string UssIconSkins = UssBlock + "__icon-skins";
		private const string UssIconBanners = UssBlock + "__icon-banners";
		private const string UssIconGliders = UssBlock + "__icon-gliders";
		private const string UssImage = UssBlock + "__image";
		private const string UssImageShadow = UssImage + "--shadow";

		private const string UssName = UssBlock + "__name";
		private const string UssNotification = UssBlock + "__notification";
		private const string UssNotificationIcon = "notification-icon";

		public GameIdGroup Category { get;  set; }
		
		private readonly VisualElement _iconSkins;
		private readonly VisualElement _iconBanners;
		private readonly VisualElement _iconGliders;
		private readonly VisualElement _image;
		private readonly VisualElement _imageShadow;
		private readonly Label _name;

		private readonly VisualElement _locked;
		private readonly VisualElement _nftBadge;
		private readonly VisualElement _loanedBadge;
		private readonly VisualElement _equippedBadge;
		private readonly VisualElement _notification;

		/// <summary>
		/// Triggered when the card is clicked
		/// </summary>
		public new event Action<GameIdGroup> clicked;

		public CollectionCategoryElement()
		{
			AddToClassList(UssBlock);
			
			var highlight = new VisualElement {name = "highlight"};
			Add(highlight);
			highlight.AddToClassList(UssHighlight);

			var background = new VisualElement {name = "background"};
			Add(background);
			background.AddToClassList(UssBackground);
			
			var selectedBg = new VisualElement {name = "selected-bg"};
			Add(selectedBg);
			selectedBg.AddToClassList(UssSelected);

			var cardHolder = new VisualElement {name = "holder"};
			Add(cardHolder);
			cardHolder.AddToClassList(UssCardHolder);
			
			cardHolder.Add(_imageShadow = new VisualElement {name = "equipment-image-shadow"});
			_imageShadow.AddToClassList(UssImage);
			_imageShadow.AddToClassList(UssImageShadow);

			cardHolder.Add(_image = new VisualElement {name = "item-image"});
			_image.AddToClassList(UssImage);
			
			cardHolder.Add(_name = new Label("CATEGORY") {name = "name"});
			_name.AddToClassList(UssName);
			
			cardHolder.Add(_iconSkins = new VisualElement {name = "icon-skins"});
			_iconSkins.AddToClassList(UssIconSkins);
			_iconSkins.visible = false;
			
			cardHolder.Add(_iconGliders = new VisualElement {name = "icon-gliders"});
			_iconGliders.AddToClassList(UssIconGliders);
			_iconGliders.visible = false;
			
			cardHolder.Add(_iconBanners = new VisualElement {name = "icon-banners"});
			_iconBanners.AddToClassList(UssIconBanners);
			_iconBanners.visible = false;
			
			cardHolder.Add(_notification = new VisualElement());
			_notification.AddToClassList(UssNotification);
			_notification.AddToClassList(UssNotificationIcon);

			base.clicked += () => clicked?.Invoke(Category);
		}

		public void SetCategory(GameIdGroup category, String text, bool notification = false)
		{
			Category = category;
			_name.text = text;
			_notification.SetDisplay(notification);

			switch (category)
			{
				case GameIdGroup.PlayerSkin: _iconSkins.visible = true; break;
				case GameIdGroup.Glider: _iconGliders.visible = true; break;
				case GameIdGroup.DeathMarker: _iconBanners.visible = true; break;
			}
		}

		public void SetSelected(bool selected)
		{
			if (selected)
			{
				AddToClassList(UssBlockSelected);
			}
			else
			{
				RemoveFromClassList(UssBlockSelected);
			}
		}

		public new class UxmlFactory : UxmlFactory<CollectionCategoryElement, UxmlTraits>
		{
		}
	}
}