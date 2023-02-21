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
	public class CollectionCardElement : Button
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

		private const string UssBadgeHolder = UssBlock + "__badge-holder";
		private const string UssBadgeNft = UssBlock + "__badge-nft";
		private const string UssBadgeLoaned = UssBlock + "__badge-loaned";
		private const string UssBadgeEquipped = UssBlock + "__badge-equipped";

		private const string UssNotification = UssBlock + "__notification";
		private const string UssNotificationIcon = "notification-icon";
		
		
		public GameIdGroup Category { get; set; }
		public GameId MenuGameId { get; private set; }

		private readonly VisualElement _image;
		private readonly VisualElement _imageShadow;
		private readonly Label _name;

		private readonly VisualElement _locked;
		private readonly VisualElement _nftBadge;
		private readonly VisualElement _loanedBadge;
		private readonly VisualElement _equippedBadge;
		private readonly VisualElement _notification;

		public CollectionCardElement()
		{
			AddToClassList(UssBlock);

			var selectedBg = new VisualElement {name = "selected-bg"};
			Add(selectedBg);
			selectedBg.AddToClassList(UssSelected);

			var highlight = new VisualElement {name = "highlight"};
			Add(highlight);
			highlight.AddToClassList(UssHighlight);

			var background = new VisualElement {name = "background"};
			Add(background);
			background.AddToClassList(UssBackground);

			var cardHolder = new VisualElement {name = "holder"};
			Add(cardHolder);
			cardHolder.AddToClassList(UssCardHolder);

			cardHolder.Add(_imageShadow = new VisualElement {name = "equipment-image-shadow"});
			_imageShadow.AddToClassList(UssImage);
			_imageShadow.AddToClassList(UssImageShadow);

			cardHolder.Add(_image = new VisualElement {name = "item-image"});
			_image.AddToClassList(UssImage);

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

			// base.clicked += () => clicked?.Invoke(Equipment, UniqueId);

			// if (highlighted)
			{
				AddToClassList(UssBlockHighlighted);
			}
			
			// base.clicked += () => clicked?.Invoke(Equipment, UniqueId);

			// if (highlighted)
			{
				// AddToClassList(UssBlockHighlighted);
			}

			// if (equipment.IsValid())
			{
				// SetCollectionElement(GameId.Male01Avatar);
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

		/// <summary>
		/// Sets the equipment item that should be displayed on this element. Use default for empty.
		/// </summary>
		public async void SetCollectionElement(GameId gameId, bool loaned = false, bool notification = false)
		{
			// var equipment = info.Equipment;
			// this.RemoveModifiers();
			// this.RemoveSpriteClasses();
			
			// Is current Collection Menu item equipped?

			// _notification.SetDisplay(notification);
			_notification.SetDisplay(true);
			MenuGameId = gameId;
			
			_name.text = gameId.GetLocalization();

			_loanedBadge.SetDisplay(true);
			_nftBadge.SetDisplay(true);

			LoadImage();
		}
		
		private async void LoadImage()
		{
			// TODO: This should be handled better.
			var services = MainInstaller.Resolve<IGameServices>();

			var sprite = await services.AssetResolverService.RequestAsset<GameId, Sprite>(MenuGameId, instantiate: false);
			_image.style.backgroundImage = new StyleBackground(sprite);
		}

		public new class UxmlFactory : UxmlFactory<CollectionCardElement, UxmlTraits>
		{
		}
	}
}