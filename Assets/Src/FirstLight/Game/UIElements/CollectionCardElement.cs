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
	public class CollectionCardElement : ImageButton
	{
		private const string EMPTY_LOC_KEY = "UITCollection/no_{0}";

		private const string ADJECTIVE_LOC_KEY = "UITCollection/adjective_{0}";

		private const string UssBlock = "collection-card";
		private const string UssBlockSelected = UssBlock + "--selected";
		private const string UssBlockHighlighted = UssBlock + "--highlighted";

		private const string UssSelected = UssBlock + "__selected-bg";
		private const string UssHighlight = UssBlock + "__highlight";
		private const string UssBackground = UssBlock + "__background";
		private const string UssCardHolder = UssBlock + "__card-holder";

		private const string UssImage = UssBlock + "__image";
		private const string UssImageShadow = UssImage + "--shadow";

		private const string UssName = UssBlock + "__name";

		private const string UssLockedIcon = UssBlock + "__locked-icon";
		private const string UssBadgeHolder = UssBlock + "__badge-holder";
		private const string UssBadgeNft = UssBlock + "__badge-nft";
		private const string UssBadgeLoaned = UssBlock + "__badge-loaned";
		private const string UssBadgeEquipped = UssBlock + "__badge-equipped";

		private const string UssNotification = UssBlock + "__notification";
		private const string UssNotificationIcon = "notification-icon";


		public GameIdGroup Category { get; set; }
		public GameId MenuGameId { get; private set; }
		public int CollectionIndex { get; private set; }

		private readonly VisualElement _backgroundImage;
		private readonly VisualElement _image;
		private readonly Label _name;

		private readonly VisualElement _locked;
		private readonly VisualElement _nftBadge;
		private readonly VisualElement _loanedBadge;
		private readonly VisualElement _equippedBadge;
		private readonly VisualElement _notification;

		/// <summary>
		/// Triggered when the card is clicked
		/// </summary>
		public new event Action<int> clicked;

		public CollectionCardElement()
		{
			AddToClassList(UssBlock);

			var selectedBg = new VisualElement {name = "selected-bg"};
			Add(selectedBg);
			selectedBg.AddToClassList(UssSelected);

			var highlight = new VisualElement {name = "highlight"};
			Add(highlight);
			highlight.AddToClassList(UssHighlight);

			_backgroundImage = new VisualElement {name = "background"};
			Add(_backgroundImage);
			_backgroundImage.AddToClassList(UssBackground);

			var cardHolder = new VisualElement {name = "holder"};
			Add(cardHolder);
			cardHolder.AddToClassList(UssCardHolder);

			cardHolder.Add(_image = new VisualElement {name = "item-image"});
			_image.AddToClassList(UssImage);

			_locked = new VisualElement {name = "locked-icon"};
			Add(_locked);
			_locked.AddToClassList((UssLockedIcon));

			var badgeHolder = new VisualElement {name = "badge-holder"};
			cardHolder.Add(badgeHolder);
			badgeHolder.AddToClassList(UssBadgeHolder);
			badgeHolder.Add(_nftBadge = new VisualElement {name = "badge-nft"});
			_nftBadge.AddToClassList(UssBadgeNft);

			badgeHolder.Add(
				_loanedBadge = new Label(ScriptLocalization.UITEquipment.loaned) {name = "badge-loaned"});
			_loanedBadge.AddToClassList(UssBadgeLoaned);

			badgeHolder.Add(
				_equippedBadge = new Label(ScriptLocalization.UITEquipment.equipped) {name = "badge-equipped"});
			_equippedBadge.AddToClassList(UssBadgeEquipped);


			cardHolder.Add(_name = new Label("COLLECTION ITEM") {name = "name"});
			_name.AddToClassList(UssName);

			cardHolder.Add(_notification = new VisualElement());
			_notification.AddToClassList(UssNotification);
			_notification.AddToClassList(UssNotificationIcon);

			base.clicked += () => clicked?.Invoke(CollectionIndex);
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

		private void SetLocked(bool locked)
		{
			_locked.visible = !locked;
			_image.style.opacity = locked ? 1f : 0.2f;
			_backgroundImage.style.color = new Color(0.1f, 0.1f, 0.1f, 1f);
		}

		/// <summary>
		/// Sets the equipment item that should be displayed on this element. Use default for empty.
		/// </summary>
		public void SetCollectionElement(GameId gameId, int index, GameIdGroup category, bool owned = false, bool equipped = false,
										 bool highlighted = false, bool isNft = false, bool loaned = false, bool notification = false)
		{
			_equippedBadge.SetDisplay(equipped);

			if (highlighted)
			{
				AddToClassList(UssBlockHighlighted);
			}

			_notification.SetDisplay(notification);
			_loanedBadge.SetDisplay(loaned);
			_nftBadge.SetDisplay(isNft);
			SetLocked(owned);

			MenuGameId = gameId;
			CollectionIndex = index;
			Category = category;

			_name.text = gameId.GetLocalization();
			_image.RemoveSpriteClasses();
			_image.AddToClassList(MenuGameId.GetUSSSpriteClass());
		}


		public new class UxmlFactory : UxmlFactory<CollectionCardElement, UxmlTraits>
		{
		}
	}
}