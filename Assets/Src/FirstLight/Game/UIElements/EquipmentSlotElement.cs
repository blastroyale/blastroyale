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
		private const string UssBlockModifier = UssBlock + "--";
		private const string UssBlockFilled = UssBlock + "--filled";
		private const string UssBlockEmpty = UssBlock + "--empty";

		private const string UssHolder = UssBlock + "__holder";
		private const string UssHolderFilled = UssHolder + "--filled";
		private const string UssHolderEmpty = UssHolder + "--empty";

		private const string UssCategoryIcon = UssBlock + "__category-icon";
		private const string UssCategoryIconEmpty = UssCategoryIcon + "--empty";
		private const string UssCategoryIconModifier = UssCategoryIcon + "--";

		private const string UssFactionIcon = UssBlock + "__faction-icon";
		private const string UssFactionIconModifier = UssFactionIcon + "--";

		private const string UssEquipmentTitle = UssBlock + "__title";
		private const string UssEquipmentTitleName = UssEquipmentTitle + "--name";
		private const string UssPlusRarity = UssBlock + "__plus-rarity";
		private const string UssEquipmentImage = UssBlock + "__equipment-image";
		private const string UssDurabilityIcon = UssBlock + "__durability-icon";
		private const string UssLevel = UssBlock + "__level";
		private const string UssDurabilityProgressBg = UssBlock + "__durability-progress-bg";
		private const string UssDurabilityProgress = UssBlock + "__durability-progress";

		public GameIdGroup Category { get; set; }

		private readonly VisualElement _categoryIcon;
		private readonly VisualElement _emptyCategoryIcon;
		private readonly Label _equipmentName;
		private readonly LocalizedLabel _emptyTitle;
		private readonly VisualElement _plusRarity;
		private readonly VisualElement _equipmentImage;
		private readonly Label _level;
		private readonly VisualElement _factionIcon;
		private readonly VisualElement _durabilityProgress;

		public EquipmentSlotElement()
		{
			AddToClassList(UssBlock);
			AddToClassList(UssBlockFilled);
			AddToClassList(UIConstants.SFX_CLICK_FORWARDS);

			var filledElement = new VisualElement {name = "filled"};
			Add(filledElement);
			filledElement.AddToClassList(UssHolder);
			filledElement.AddToClassList(UssHolderFilled);

			filledElement.Add(_categoryIcon = new VisualElement {name = "category"});
			_categoryIcon.AddToClassList(UssCategoryIcon);

			filledElement.Add(_equipmentName = new Label("APO SNIPER") {name = "equipment-name"});
			_equipmentName.AddToClassList(UssEquipmentTitle);
			_equipmentName.AddToClassList(UssEquipmentTitleName);

			filledElement.Add(_plusRarity = new VisualElement {name = "plus-rarity"});
			_plusRarity.AddToClassList(UssPlusRarity);

			filledElement.Add(_equipmentImage = new VisualElement {name = "equipment-image"});
			_equipmentImage.AddToClassList(UssEquipmentImage);

			var durabilityIcon = new VisualElement {name = "durability-icon"};
			filledElement.Add(durabilityIcon);
			durabilityIcon.AddToClassList(UssDurabilityIcon);

			filledElement.Add(_level = new Label(string.Format(ScriptLocalization.UITEquipment.lvl, "5"))
				{name = "level"});
			_level.AddToClassList(UssLevel);

			filledElement.Add(_factionIcon = new VisualElement {name = "faction-icon"});
			_factionIcon.AddToClassList(UssFactionIcon);

			var durabilityProgressBg = new VisualElement {name = "durability-progress-bg"};
			filledElement.Add(durabilityProgressBg);
			durabilityProgressBg.AddToClassList(UssDurabilityProgressBg);

			durabilityProgressBg.Add(_durabilityProgress = new VisualElement {name = "durability-progress"});
			_durabilityProgress.AddToClassList(UssDurabilityProgress);

			var emptyElement = new VisualElement {name = "empty"};
			Add(emptyElement);
			emptyElement.AddToClassList(UssHolder);
			emptyElement.AddToClassList(UssHolderEmpty);

			emptyElement.Add(_emptyCategoryIcon = new VisualElement {name = "category"});
			_emptyCategoryIcon.AddToClassList(UssCategoryIcon);
			_emptyCategoryIcon.AddToClassList(UssCategoryIconEmpty);

			emptyElement.Add(_emptyTitle = new LocalizedLabel {name = "title"});
			_emptyTitle.AddToClassList(UssEquipmentTitle);
		}


		/// <summary>
		/// Sets the equipment item that should be displayed on this element. Use default for empty.
		/// </summary>
		public async void SetEquipment(Equipment equipment)
		{
			this.RemoveModifiers();

			if (equipment.IsValid() && !equipment.IsDefaultItem())
			{
				AddToClassList(UssBlockFilled);
				AddToClassList(UssBlockModifier + equipment.Rarity.ToString().ToLowerInvariant().Replace("plus", ""));

				_equipmentName.text = equipment.GameId.GetTranslation();

				_factionIcon.RemoveModifiers();
				_factionIcon.AddToClassList(UssFactionIconModifier + equipment.Faction.ToString().ToLowerInvariant());

				var durability = (float) equipment.Durability / equipment.MaxDurability;
				_durabilityProgress.style.flexGrow = durability;

				_plusRarity.style.display = (int) equipment.Rarity % 2 == 1 ? DisplayStyle.Flex : DisplayStyle.None;

				_level.text = string.Format(ScriptLocalization.UITEquipment.lvl, equipment.Level);

				// TODO: This should be handled better.
				var services = MainInstaller.Resolve<IGameServices>();
				var sprite = await services.AssetResolverService.RequestAsset<GameId, Sprite>(
					equipment.GameId, instantiate: false);
				_equipmentImage.style.backgroundImage = new StyleBackground(sprite);
			}
			else
			{
				AddToClassList(UssBlockEmpty);
			}
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

				ece.Category = cat;
				ece._categoryIcon.AddToClassList(UssCategoryIconModifier + cat.ToString().ToLowerInvariant());

				ece._emptyTitle.Localize(
					string.Format(EMPTY_LOC_KEY, cat.ToString().ToLowerInvariant())
				);
			}
		}
	}
}