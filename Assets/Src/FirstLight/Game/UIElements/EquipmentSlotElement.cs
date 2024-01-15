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
	/// Displays an equipment category button
	/// </summary>
	public class EquipmentSlotElement : Button
	{
		private const string EMPTY_LOC_KEY = "UITEquipment/no_{0}";

		private const string UssBlock = "equipment-slot";
		private const string UssBlockFilled = UssBlock + "--filled";
		private const string UssBlockEmpty = UssBlock + "--empty";

		private const string UssHolder = UssBlock + "__holder";
		private const string UssHolderFilled = UssHolder + "--filled";
		private const string UssHolderEmpty = UssHolder + "--empty";

		private const string UssCategoryIcon = UssBlock + "__category-icon";
		private const string UssCategoryIconEmpty = UssCategoryIcon + "--empty";

		private const string UssFactionIcon = UssBlock + "__faction-icon";

		private const string UssBadgeHolder = UssBlock + "__badge-holder";
		private const string UssBadge = UssBlock + "__badge";
		private const string UssBadgeNft = UssBadge + "--nft";
		private const string UssBadgeLoaned = UssBadge + "--loaned";

		private const string UssEquipmentLevel = UssBlock + "__level";
		private const string UssEquipmentTitle = UssBlock + "__title";
		private const string UssEquipmentTitleName = UssEquipmentTitle + "--name";

		private const string UssPlusRarity = UssBlock + "__plus-rarity";

		private const string UssEquipmentImage = UssBlock + "__equipment-image";
		private const string UssEquipmentImageEmpty = UssEquipmentImage + "--empty";
		private const string UssEquipmentImageShadow = UssEquipmentImage + "--shadow";

		private const string UssNotification = UssBlock + "__notification";
		private const string UssNotificationIcon = "notification-icon";

		private const string UssSpriteSlotRarity = "sprite-home__button-equipmentslot-{0}";
		private const string UssSpriteEquipmentCategory = "sprite-shared__icon-equipmentcategory-{0}";
		private const string UssSpriteFaction = "sprite-equipmentcard__card-faction-{0}";

		public GameIdGroup Category { get; set; }

		private readonly VisualElement _categoryIcon;
		private readonly VisualElement _emptyCategoryIcon;
		private readonly Label _equipmentName;
		private readonly Label _equipmentLevel;

		private readonly LocalizedLabel _emptyTitle;
		private readonly VisualElement _plusRarity;
		private readonly VisualElement _equipmentImage;
		private readonly VisualElement _equipmentImageShadow;
		private readonly VisualElement _factionIcon;
		private readonly VisualElement _badgeNft;
		private readonly VisualElement _badgeLoaned;
		private readonly VisualElement _notificationIcon;

		public EquipmentSlotElement()
		{
			AddToClassList(UssBlock);
			AddToClassList(UssBlockFilled);
			AddToClassList(UIConstants.SFX_CLICK_FORWARDS);
			AddToClassList(string.Format(UssSpriteSlotRarity, "common"));

			var filledElement = new VisualElement {name = "filled"};
			{
				Add(filledElement);
				filledElement.AddToClassList(UssHolder);
				filledElement.AddToClassList(UssHolderFilled);

				filledElement.Add(_categoryIcon = new VisualElement {name = "category"});
				_categoryIcon.AddToClassList(UssCategoryIcon);
				_categoryIcon.AddToClassList(string.Format(UssSpriteEquipmentCategory, "weapon"));

				filledElement.Add(_equipmentName = new AutoSizeLabel(
					string.Format(ScriptLocalization.UITEquipment.item_name_lvl, "SOOPERDOOPER LOOTERSHOOTER"),
					35, 45
				) {name = "equipment-name"});
				_equipmentName.AddToClassList(UssEquipmentTitle);
				_equipmentName.AddToClassList(UssEquipmentTitleName);

				filledElement.Add(_equipmentLevel =
					new Label(string.Format(ScriptLocalization.UITEquipment.card_lvl, 17))
						{name = "level"});
				_equipmentLevel.AddToClassList(UssEquipmentLevel);

				filledElement.Add(_plusRarity = new VisualElement {name = "plus-rarity"});
				_plusRarity.AddToClassList(UssPlusRarity);

				filledElement.Add(_equipmentImageShadow = new VisualElement {name = "equipment-image-shadow"});
				_equipmentImageShadow.AddToClassList(UssEquipmentImage);
				_equipmentImageShadow.AddToClassList(UssEquipmentImageShadow);

				filledElement.Add(_equipmentImage = new VisualElement {name = "equipment-image"});
				_equipmentImage.AddToClassList(UssEquipmentImage);

				var badgeHolder = new VisualElement {name = "badge-holder"};
				{
					filledElement.Add(badgeHolder);
					badgeHolder.AddToClassList(UssBadgeHolder);

					badgeHolder.Add(_badgeNft = new VisualElement {name = "badge-nft"});
					_badgeNft.AddToClassList(UssBadge);
					_badgeNft.AddToClassList(UssBadgeNft);

					badgeHolder.Add(
						_badgeLoaned = new Label(ScriptLocalization.UITEquipment.loaned) {name = "badge-loaned"});
					_badgeLoaned.AddToClassList(UssBadge);
					_badgeLoaned.AddToClassList(UssBadgeLoaned);
				}

				filledElement.Add(_factionIcon = new VisualElement {name = "faction-icon"});
				_factionIcon.RemoveSpriteClasses();
				_factionIcon.AddToClassList(UssFactionIcon);
				_factionIcon.AddToClassList(string.Format(UssSpriteFaction, "celestial"));
			}

			var emptyElement = new VisualElement {name = "empty"};
			{
				Add(emptyElement);
				emptyElement.AddToClassList(UssHolder);
				emptyElement.AddToClassList(UssHolderEmpty);

				emptyElement.Add(_emptyCategoryIcon = new VisualElement {name = "category"});
				_emptyCategoryIcon.AddToClassList(UssCategoryIcon);
				_emptyCategoryIcon.AddToClassList(UssCategoryIconEmpty);

				emptyElement.Add(_emptyTitle = new LocalizedLabel {name = "title"});
				_emptyTitle.AddToClassList(UssEquipmentTitle);
			}

			Add(_notificationIcon = new VisualElement {name = "notification"});
			_notificationIcon.AddToClassList(UssNotification);
			_notificationIcon.AddToClassList(UssNotificationIcon);
		}

		/// <summary>
		/// Sets the equipment item that should be displayed on this element. Use default for empty.
		/// </summary>
		public async void SetEquipment(EquipmentInfo info, bool loaned, bool notification)
		{
			var equipment = info.Equipment;
			this.RemoveModifiers();
			this.RemoveSpriteClasses();

			_notificationIcon.SetDisplay(notification);

			if (!equipment.IsValid() && !equipment.IsDefaultItem())
			{
				AddToClassList(UssBlockEmpty);
				return;
			}

			AddToClassList(UssBlockFilled);
			AddToClassList(string.Format(UssSpriteSlotRarity,
				equipment.Rarity.ToString().ToLowerInvariant().Replace("plus", "")));

			_equipmentName.text = string.Format(ScriptLocalization.UITEquipment.item_name_lvl,
				equipment.GameId.GetLocalization());
			_equipmentLevel.text = string.Format(ScriptLocalization.UITEquipment.card_lvl, equipment.Level);

			_factionIcon.RemoveSpriteClasses();
			_factionIcon.AddToClassList(
				string.Format(UssSpriteFaction, equipment.Faction.ToString().ToLowerInvariant()));
			_factionIcon.SetDisplay(info.IsNft);

			_plusRarity.SetDisplay((int) equipment.Rarity % 2 == 1);

			_badgeNft.SetDisplay(info.IsNft);
			_badgeLoaned.SetDisplay(loaned);

			// TODO: This should be handled better.
			var services = MainInstaller.Resolve<IGameServices>();
			var sprite = await services.AssetResolverService.RequestAsset<GameId, Sprite>(
				equipment.GameId, instantiate: false);
			_equipmentImage.style.backgroundImage =
				_equipmentImageShadow.style.backgroundImage = new StyleBackground(sprite);
		}

		public new class UxmlFactory : UxmlFactory<EquipmentSlotElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlEnumAttributeDescription<GameIdGroup> _categoryAttribute = new()
			{
				name = "category",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = GameIdGroup.Weapon
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var ece = (EquipmentSlotElement) ve;
				var cat = _categoryAttribute.GetValueFromBag(bag, cc);
				var catStr = cat.ToString().ToLowerInvariant();

				ece.Category = cat;

				var catIcon = string.Format(UssSpriteEquipmentCategory, catStr);
				ece._emptyCategoryIcon.RemoveSpriteClasses();
				ece._emptyCategoryIcon.AddToClassList(catIcon);
				ece._categoryIcon.RemoveSpriteClasses();
				ece._categoryIcon.AddToClassList(catIcon);

				ece._emptyTitle.Localize(
					string.Format(EMPTY_LOC_KEY, cat.ToString().ToLowerInvariant())
				);
			}
		}
	}
}