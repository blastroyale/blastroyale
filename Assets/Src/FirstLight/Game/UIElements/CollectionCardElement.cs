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
		private const string UssRarity = UssBlock + "__rarity";
		private const string UssCardHolder = UssBlock + "__card-holder";
		private const string UssPlusRarity = UssBlock + "__plus-rarity";
		private const string UssImage = UssBlock + "__image";
		private const string UssImageShadow = UssImage + "--shadow";
		private const string UssGrade = UssBlock + "__grade";
		private const string UssFaction = UssBlock + "__faction";
		private const string UssMaterial = UssBlock + "__material";
		private const string UssLevel = UssBlock + "__level";
		private const string UssName = UssBlock + "__name";
		private const string UssAdjCatHolder = UssBlock + "__adj-cat-holder";
		private const string UssCategory = UssBlock + "__category";
		private const string UssAdjective = UssBlock + "__adjective";
		private const string UssBadgeHolder = UssBlock + "__badge-holder";
		private const string UssBadgeNft = UssBlock + "__badge-nft";
		private const string UssBadgeLoaned = UssBlock + "__badge-loaned";
		private const string UssBadgeEquipped = UssBlock + "__badge-equipped";

		private const string UssNotification = UssBlock + "__notification";
		private const string UssNotificationIcon = "notification-icon";
		
		private const string UssSpriteRarity = "sprite-collectioncard__card-rarity-{0}";
		private const string UssSpriteFaction = "sprite-collectioncard__card-faction-{0}";
		private const string UssSpriteMaterial = "sprite-collectioncard__card-material-{0}";
		private const string UssSpriteCategory = "sprite-collectioncard__card-category-{0}";
		
		public GameIdGroup Category { get; set; }
		public GameId MenuGameId { get; private set; }

		private readonly VisualElement _image;
		private readonly VisualElement _imageShadow;
		private readonly Label _name;

		private readonly VisualElement _locked;
		private readonly VisualElement _nftBadge;
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

			// cardHolder.Add(_rarity = new VisualElement {name = "rarity"});
			// _rarity.AddToClassList(UssRarity);
			// _rarity.AddToClassList(string.Format(UssSpriteRarity, "common"));

			// cardHolder.Add(_plusRarity = new VisualElement {name = "plus-rarity"});
			// _plusRarity.AddToClassList(UssPlusRarity);

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

			//badgeHolder.Add(
			//	_loanedBadge = new Label(ScriptLocalization.UITEquipment.loaned) {name = "badge-loaned"});
			// _loanedBadge.AddToClassList(UssBadgeLoaned);

			badgeHolder.Add(
				_equippedBadge = new Label(ScriptLocalization.UITEquipment.equipped) {name = "badge-equipped"});
			_equippedBadge.AddToClassList(UssBadgeEquipped);
			

			// cardHolder.Add(_grade = new Label("IV") {name = "grade"});
			// _grade.AddToClassList(UssGrade);

			// cardHolder.Add(_faction = new VisualElement {name = "faction"});
			// _faction.AddToClassList(UssFaction);
			// _faction.AddToClassList(string.Format(UssSpriteFaction, "dimensional"));

			// cardHolder.Add(_material = new VisualElement {name = "material"});
			// _material.AddToClassList(UssMaterial);
			// _material.AddToClassList(string.Format(UssSpriteMaterial, "bronze"));

			// cardHolder.Add(_level = new Label(string.Format(ScriptLocalization.UITEquipment.card_lvl, "15"))
			//	{name = "level"});
			// _level.AddToClassList(UssLevel);

			cardHolder.Add(_name = new Label("COLLECTION ITEM") {name = "name"});
			_name.AddToClassList(UssName);

			/*
			var adjCatHolder = new VisualElement {name = "adjective-category-holder"};
			cardHolder.Add(adjCatHolder);
			adjCatHolder.AddToClassList(UssAdjCatHolder);
			{
				adjCatHolder.Add(_category = new VisualElement {name = "category"});
				_category.AddToClassList(UssCategory);
				_category.AddToClassList(string.Format(UssSpriteCategory, "weapon"));

				adjCatHolder.Add(_adjective = new Label("MAGNIFICENT") {name = "adjective"});
				_adjective.AddToClassList(UssAdjective);
			}
			*/
			
			cardHolder.Add(_notification = new VisualElement());
			_notification.AddToClassList(UssNotification);
			_notification.AddToClassList(UssNotificationIcon);

			// base.clicked += () => clicked?.Invoke(Equipment, UniqueId);

			// if (highlighted)
			{
				AddToClassList(UssBlockHighlighted);
			}

			// if (equipment.IsValid())
			{
				// SetEquipment(equipment, UniqueId.Invalid);
			}
			
			/*
			AddToClassList(UssBlock);

			var selectedBg = new VisualElement {name = "--selected"};
			Add(selectedBg);
			selectedBg.AddToClassList(UssBlockSelected);

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
			{
				badgeHolder.Add(_nftBadge = new VisualElement {name = "badge-nft"});
				_nftBadge.AddToClassList(UssBadgeNft);

				badgeHolder.Add(
					_equippedBadge = new Label(ScriptLocalization.UITEquipment.equipped) {name = "badge-equipped"});
				_equippedBadge.AddToClassList(UssBadgeEquipped);
			}

			cardHolder.Add(_name = new Label("COLLECTION ITEM") {name = "name"});
			_name.AddToClassList(UssName);

			cardHolder.Add(_notification = new VisualElement());
			_notification.AddToClassList(UssNotification);
			_notification.AddToClassList(UssNotificationIcon);
			
			cardHolder.Add(_locked = new VisualElement{name = "--locked"});
			_locked.AddToClassList(UssBlockLocked);
			*/
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