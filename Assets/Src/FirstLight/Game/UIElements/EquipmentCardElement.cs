using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays an equipment card with badges / highlights / selection.
	/// </summary>
	public class EquipmentCardElement : ImageButton
	{
		private const string ADJECTIVE_LOC_KEY = "UITEquipment/adjective_{0}";

		private const string UssBlock = "equipment-card";
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

		private const string UssSpriteRarity = "sprite-equipmentcard__card-rarity-{0}";
		private const string UssSpriteFaction = "sprite-equipmentcard__card-faction-{0}";
		private const string UssSpriteMaterial = "sprite-equipmentcard__card-material-{0}";
		private const string UssSpriteCategory = "sprite-equipmentcard__card-category-{0}";

		public Equipment Equipment { get; private set; }
		public UniqueId UniqueId { get; private set; }

		private readonly VisualElement _nftBadge;
		private readonly VisualElement _loanedBadge;
		private readonly VisualElement _equippedBadge;

		private readonly VisualElement _image;
		private readonly VisualElement _imageShadow;
		private readonly VisualElement _plusRarity;
		private readonly VisualElement _rarity;
		private readonly VisualElement _faction;
		private readonly VisualElement _material;
		private readonly VisualElement _category;
		private readonly VisualElement _notification;

		private readonly Label _grade;
		private readonly Label _level;
		private readonly Label _name;
		private readonly Label _adjective;

		/// <summary>
		/// Triggered when the card is clicked
		/// </summary>
		public new event Action<Equipment, UniqueId> clicked;

		public EquipmentCardElement() : this(Equipment.None, UniqueId.Invalid)
		{
		}

		public EquipmentCardElement(Equipment equipment, UniqueId id, bool highlighted = false)
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

			cardHolder.Add(_rarity = new VisualElement {name = "rarity"});
			_rarity.AddToClassList(UssRarity);
			_rarity.AddToClassList(string.Format(UssSpriteRarity, "common"));

			cardHolder.Add(_plusRarity = new VisualElement {name = "plus-rarity"});
			_plusRarity.AddToClassList(UssPlusRarity);

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
					_loanedBadge = new Label(ScriptLocalization.UITEquipment.loaned) {name = "badge-loaned"});
				_loanedBadge.AddToClassList(UssBadgeLoaned);

				badgeHolder.Add(
					_equippedBadge = new Label(ScriptLocalization.UITEquipment.packed) {name = "badge-equipped"});
				_equippedBadge.AddToClassList(UssBadgeEquipped);
			}

			cardHolder.Add(_grade = new Label("IV") {name = "grade"});
			_grade.AddToClassList(UssGrade);

			cardHolder.Add(_faction = new VisualElement {name = "faction"});
			_faction.AddToClassList(UssFaction);
			_faction.AddToClassList(string.Format(UssSpriteFaction, "dimensional"));

			cardHolder.Add(_material = new VisualElement {name = "material"});
			_material.AddToClassList(UssMaterial);
			_material.AddToClassList(string.Format(UssSpriteMaterial, "bronze"));

			cardHolder.Add(_level = new Label(string.Format(ScriptLocalization.UITEquipment.card_lvl, "15"))
				{name = "level"});
			_level.AddToClassList(UssLevel);

			cardHolder.Add(_name = new Label("ROCKET LAUNCHER") {name = "name"});
			_name.AddToClassList(UssName);

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

			cardHolder.Add(_notification = new VisualElement());
			_notification.AddToClassList(UssNotification);
			_notification.AddToClassList(UssNotificationIcon);

			base.clicked += () => clicked?.Invoke(Equipment, UniqueId);

			if (highlighted)
			{
				AddToClassList(UssBlockHighlighted);
			}

			if (equipment.IsValid())
			{
				SetEquipment(equipment, id);
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

		public void SetEquipment(Equipment equipment, UniqueId id, bool loaned = false, bool nft = false,
								 bool equipped = false, bool notification = false, bool loadEditorSprite = false)
		{
			Assert.IsTrue(equipment.IsValid());

			_loanedBadge.SetDisplay(loaned);
			_nftBadge.SetDisplay(nft);
			_equippedBadge.SetDisplay(equipped);
			_notification.SetDisplay(notification);

			_rarity.RemoveSpriteClasses();
			_rarity.AddToClassList(string.Format(UssSpriteRarity,
				equipment.Rarity.ToString().Replace("Plus", "").ToLowerInvariant()));

			_plusRarity.SetDisplay((int) equipment.Rarity % 2 == 1);

			_grade.text = equipment.Grade.ToString().Replace("Grade", "");

			_material.RemoveSpriteClasses();
			_material.AddToClassList(string.Format(UssSpriteMaterial,
				equipment.Material.ToString().ToLowerInvariant()));

			_faction.RemoveSpriteClasses();
			_faction.AddToClassList(string.Format(UssSpriteFaction, equipment.Faction.ToString().ToLowerInvariant()));

			_level.text = string.Format(ScriptLocalization.UITEquipment.card_lvl, equipment.Level);
			_name.text = equipment.GameId.GetLocalization();

			_category.RemoveSpriteClasses();
			_category.AddToClassList(string.Format(UssSpriteCategory,
				equipment.GetEquipmentGroup().ToString().ToLowerInvariant()));

			_adjective.text = equipment.Adjective.ToString().ToUpperInvariant(); // TODO: Add localization
			_adjective.text = string.Format(ADJECTIVE_LOC_KEY, equipment.Adjective.ToString().ToLowerInvariant())
				.LocalizeKey();

			UniqueId = id;
			var shouldLoadImage = equipment.IsValid() && equipment.GameId != Equipment.GameId;
			Equipment = equipment;
			if (shouldLoadImage) LoadImage(loadEditorSprite);
		}

		private async void LoadImage(bool loadEditorSprite)
		{
#if UNITY_EDITOR
			if (loadEditorSprite)
			{
				_image.style.backgroundImage =
					_imageShadow.style.backgroundImage = new StyleBackground(
						UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
							$"Assets/AddressableResources/Sprites/Equipment/{Equipment.GetEquipmentGroup().ToString()}/{Equipment.GameId.ToString()}.png"));
				return;
			}
#endif
			await UIUtils.SetSprite(Equipment.GameId, _image, _imageShadow);
		}

		public new class UxmlFactory : UxmlFactory<EquipmentCardElement, UxmlTraits>
		{
		}
	}
}