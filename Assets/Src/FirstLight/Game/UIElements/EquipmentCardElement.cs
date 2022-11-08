using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.UIElements
{
	public class EquipmentCardElement : ImageButton
	{
		private const string UssBlock = "equipment-card";

		private const string UssRarity = UssBlock + "__rarity";
		private const string UssRarityModifier = UssRarity + "--";
		private const string UssCardHolder = UssBlock + "__card-holder";
		private const string UssPlusRarity = UssBlock + "__plus-rarity";
		private const string UssImage = UssBlock + "__image";
		private const string UssImageShadow = UssImage + "--shadow";
		private const string UssGrade = UssBlock + "__grade";
		private const string UssFaction = UssBlock + "__faction";
		private const string UssFactionModifier = UssBlock + "--";
		private const string UssMaterial = UssBlock + "__material";
		private const string UssMaterialModifier = UssMaterial + "--";
		private const string UssLevel = UssBlock + "__level";
		private const string UssName = UssBlock + "__name";
		private const string UssAdjCatHolder = UssBlock + "__adj-cat-holder";
		private const string UssCategory = UssBlock + "__category";
		private const string UssCategoryModifier = UssCategory + "--";
		private const string UssAdjective = UssBlock + "__adjective";
		private const string UssBadgeHolder = UssBlock + "__badge-holder";
		private const string UssBadgeNft = UssBlock + "__badge-nft";
		private const string UssBadgeLoaned = UssBlock + "__badge-loaned";


		private VisualElement _nftBadge;
		private VisualElement _loanedBadge;

		private VisualElement _image;
		private VisualElement _imageShadow;
		private VisualElement _plusRarity;
		private VisualElement _rarity;
		private VisualElement _faction;
		private VisualElement _material;
		private VisualElement _category;

		private Label _grade;
		private Label _level;
		private Label _name;
		private Label _adjective;


		public EquipmentCardElement() : this(Equipment.None)
		{
		}

		public EquipmentCardElement(Equipment equipment)
		{
			AddToClassList(UssBlock);

			var cardHolder = new VisualElement {name = "holder"};
			Add(cardHolder);
			cardHolder.AddToClassList(UssCardHolder);

			cardHolder.Add(_rarity = new VisualElement {name = "rarity"});
			_rarity.AddToClassList(UssRarity);

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
			}

			cardHolder.Add(_grade = new Label("IV") {name = "grade"});
			_grade.AddToClassList(UssGrade);

			cardHolder.Add(_faction = new VisualElement {name = "faction"});
			_faction.AddToClassList(UssFaction);

			cardHolder.Add(_material = new VisualElement {name = "material"});
			_material.AddToClassList(UssMaterial);

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

				adjCatHolder.Add(_adjective = new Label("MAGNIFICENT") {name = "adjective"});
				_adjective.AddToClassList(UssAdjective);
			}

			if (equipment.IsValid())
			{
				SetEquipment(equipment);
			}
		}

		public async void SetEquipment(Equipment equipment, bool loaned = false, bool nft = false)
		{
			Assert.IsTrue(equipment.IsValid());

			_rarity.RemoveModifiers();
			_rarity.AddToClassList(UssRarityModifier +
				equipment.Rarity.ToString().Replace("Plus", "").ToLowerInvariant());

			_plusRarity.SetDisplayActive((int) equipment.Rarity % 2 == 1);

			_grade.text = equipment.Grade.ToString().Replace("Grade", "");

			_material.RemoveModifiers();
			_material.AddToClassList(UssMaterialModifier + equipment.Material.ToString().ToLowerInvariant());

			_faction.RemoveModifiers();
			_faction.AddToClassList(UssFactionModifier + equipment.Faction.ToString().ToLowerInvariant());

			_level.text = string.Format(ScriptLocalization.UITEquipment.card_lvl, equipment.Level);
			_name.text = equipment.GameId.GetTranslation();

			_category.RemoveModifiers();
			_category.AddToClassList(UssCategoryModifier + equipment.GetEquipmentGroup().ToString().ToLowerInvariant());

			_adjective.text = equipment.Adjective.ToString().ToUpperInvariant(); // TODO: Add localization

			_loanedBadge.SetDisplayActive(loaned);
			_nftBadge.SetDisplayActive(nft);

			// TODO: This should be handled better.
			var services = MainInstaller.Resolve<IGameServices>();
			var sprite = await services.AssetResolverService.RequestAsset<GameId, Sprite>(
				equipment.GameId, instantiate: false);
			_image.style.backgroundImage =
				_imageShadow.style.backgroundImage = new StyleBackground(sprite);
		}

		public new class UxmlFactory : UxmlFactory<EquipmentCardElement, UxmlTraits>
		{
		}
	}
}