using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
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

		public ItemData Item { get; private set; }
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
		public event Action<int> Clicked;

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

			SetNotificationPip(false);
			SetLoanedIcon(false);
			SetIsNft(false);
			clicked += () => Clicked?.Invoke(CollectionIndex);
		}

		public void SetSelected(bool selected)
		{
			if (selected)
			{
				AddToClassList(UssBlockSelected);
				if (_notification.style.display == DisplayStyle.Flex)
				{
					MainInstaller.ResolveServices().RewardService.MarkAsSeen(ItemMetadataType.Collection, Item);
					SetNotificationPip(false);
				}
			}
			else RemoveFromClassList(UssBlockSelected);
		}

		private void SetLocked(bool locked)
		{
			_locked.visible = !locked;
			_image.style.opacity = locked ? 1f : 0.2f;
			_backgroundImage.style.color = new Color(0.1f, 0.1f, 0.1f, 1f);
		}

		public void SetNotificationPip(bool b) => _notification.SetDisplay(b);
		public void SetLoanedIcon(bool b) => _loanedBadge.SetDisplay(b);
		public void SetIsNft(bool b) => _nftBadge.SetDisplay(b);
		public void SetIsEquipped(bool b) => _equippedBadge.SetDisplay(b);
		public void SetIsOwned(bool b) => SetLocked(b);

		public void SetHighlight(bool b)
		{
			if (b) AddToClassList(UssBlockHighlighted);
			else RemoveFromClassList(UssBlockHighlighted);
		}

		public void SetCollectionElement(ItemData item, string displayName, int index, bool owned = false)
		{
			Item = item;
			var view = item.GetViewModel();
			CollectionIndex = index;

			if (item.Id.IsInGroup(GameIdGroup.ProfilePicture))
			{
				_name.text = "";
			}
			else
			{
				_name.text = displayName;
			}

			view.DrawIcon(_image);
		}

		public new class UxmlFactory : UxmlFactory<CollectionCardElement, AutoFocusTrait>
		{
		}
	}
}