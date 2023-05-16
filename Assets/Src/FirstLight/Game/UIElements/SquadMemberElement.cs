using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class SquadMemberElement : VisualElement
	{
		private const string USS_BLOCK = "squad-member";
		private const string USS_BG = USS_BLOCK + "__bg";
		private const string USS_PFP = USS_BLOCK + "__pfp";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_MIGHT = USS_BLOCK + "__might";
		private const string USS_SHIELD_HEALTH_CONTAINER = USS_BLOCK + "__shield-health-container";
		private const string USS_SHIELD_HEALTH_BG = USS_BLOCK + "__shield-health-bg";
		private const string USS_SHIELD_BAR = USS_BLOCK + "__shield-bar";
		private const string USS_HEALTH_BAR = USS_BLOCK + "__health-bar";
		private const string USS_EQUIPMENT_CONTAINER = USS_BLOCK + "__equipment-container";
		private const string USS_EQUIPMENT = USS_BLOCK + "__equipment";

		private VisualElement _bg;
		private VisualElement _pfp;
		private Label _might;
		private Label _name;

		private VisualElement _shieldBar;
		private VisualElement _healthBar;

		private VisualElement _equipmentWeapon;
		private VisualElement _equipmentHelmet;
		private VisualElement _equipmentShield;
		private VisualElement _equipmentAmulet;
		private VisualElement _equipmentArmor;

		public SquadMemberElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_bg = new VisualElement {name = "bg"});
			_bg.AddToClassList(USS_BG);

			Add(_pfp = new VisualElement {name = "pfp"});
			_pfp.AddToClassList(USS_PFP);

			Add(_might = new Label("1324") {name = "might"});
			_might.AddToClassList(USS_MIGHT);

			Add(_name = new Label("PLAYER NAME") {name = "name"});
			_name.AddToClassList(USS_NAME);

			var shieldHealthContainer = new VisualElement {name = "shieldhealth-container"};
			Add(shieldHealthContainer);
			shieldHealthContainer.AddToClassList(USS_SHIELD_HEALTH_CONTAINER);
			{
				var shieldBg = new VisualElement {name = "shield-bg"};
				shieldHealthContainer.Add(shieldBg);
				shieldBg.AddToClassList(USS_SHIELD_HEALTH_BG);
				{
					shieldBg.Add(_shieldBar = new VisualElement {name = "shield-bar"});
					_shieldBar.AddToClassList(USS_SHIELD_BAR);
				}

				var healthBg = new VisualElement {name = "health-bg"};
				shieldHealthContainer.Add(healthBg);
				healthBg.AddToClassList(USS_SHIELD_HEALTH_BG);
				{
					healthBg.Add(_healthBar = new VisualElement {name = "health-bar"});
					_healthBar.AddToClassList(USS_HEALTH_BAR);
				}
			}

			var equipmentContainer = new VisualElement {name = "equipment-container"};
			Add(equipmentContainer);
			equipmentContainer.AddToClassList(USS_EQUIPMENT_CONTAINER);
			{
				equipmentContainer.Add(_equipmentWeapon = new VisualElement {name = "weapon"});
				_equipmentWeapon.AddToClassList(USS_EQUIPMENT);

				equipmentContainer.Add(_equipmentHelmet = new VisualElement {name = "helmet"});
				_equipmentHelmet.AddToClassList(USS_EQUIPMENT);

				equipmentContainer.Add(_equipmentShield = new VisualElement {name = "shield"});
				_equipmentShield.AddToClassList(USS_EQUIPMENT);

				equipmentContainer.Add(_equipmentAmulet = new VisualElement {name = "amulet"});
				_equipmentAmulet.AddToClassList(USS_EQUIPMENT);

				equipmentContainer.Add(_equipmentArmor = new VisualElement {name = "armor"});
				_equipmentArmor.AddToClassList(USS_EQUIPMENT);
			}
		}

		public void SetPlayer(EntityRef entity)
		{
		}

		public new class UxmlFactory : UxmlFactory<SquadMemberElement, UxmlTraits>
		{
		}

		public void UpdateEquipment(Equipment equipment)
		{
			throw new System.NotImplementedException();
		}
	}
}