using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	public class SquadMemberElement : VisualElement
	{
		private const int DAMAGE_ANIMATION_DURATION = 500;

		private readonly Color DAMAGE_BG = new(212f / 255f, 29f / 255f, 27f / 255f, 0.5f);
		private readonly Color NORMAL_BG = new(49f / 255f, 45f / 255f, 71f / 255f, 0.5f);

		private const string USS_BLOCK = "squad-member";
		private const string USS_CONTAINER = USS_BLOCK + "__container";
		private const string USS_DEAD = USS_BLOCK + "--dead";
		private const string USS_DEAD_CROSS = USS_BLOCK + "__dead-cross";
		private const string USS_BG = USS_BLOCK + "__bg";
		private const string USS_PFP = USS_BLOCK + "__pfp";
		private const string USS_TEAMCOLOR = USS_BLOCK + "__team-color";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_LEVEL = USS_BLOCK + "__level";
		private const string USS_SHIELD_HEALTH_CONTAINER = USS_BLOCK + "__shield-health-container";
		private const string USS_SHIELD_HEALTH_BG = USS_BLOCK + "__shield-health-bg";
		private const string USS_SHIELD_BAR = USS_BLOCK + "__shield-bar";
		private const string USS_HEALTH_BAR = USS_BLOCK + "__health-bar";
		private const string USS_EQUIPMENT_CONTAINER = USS_BLOCK + "__equipment-container";
		private const string USS_EQUIPMENT = USS_BLOCK + "__equipment";
		private const string USS_EQUIPMENT_ACQUIRED = USS_EQUIPMENT + "--acquired";

		private const string USS_SPRITE_EQUIPMENTCATEGORY = "sprite-shared__icon-equipmentcategory-{0}";

		private VisualElement _container;
		private VisualElement _bg;
		private VisualElement _pfp;
		private VisualElement _teamColor;
		private Label _level;
		private Label _name;

		private VisualElement _shieldBar;
		private VisualElement _healthBar;

		private VisualElement _equipmentWeapon;
		private VisualElement _equipmentHelmet;
		private VisualElement _equipmentShield;
		private VisualElement _equipmentAmulet;
		private VisualElement _equipmentArmor;

		private PlayerRef _player;
		private int _pfpRequestHandle;

		private readonly ValueAnimation<float> _damageAnimation;
		private readonly IVisualElementScheduledItem _damageAnimationHandle;

		public SquadMemberElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_container = new VisualElement {name = "container"});
			_container.AddToClassList(USS_CONTAINER);

			var deadCross = new VisualElement {name = "dead-cross"};
			Add(deadCross);
			deadCross.AddToClassList(USS_DEAD_CROSS);

			_container.Add(_bg = new VisualElement {name = "bg"});
			_bg.AddToClassList(USS_BG);
			
			_container.Add(_teamColor = new VisualElement {name = "teamColor"});
			_teamColor.AddToClassList(USS_TEAMCOLOR);
			
			_container.Add(_pfp = new VisualElement {name = "pfp"});
			_pfp.AddToClassList(USS_PFP);
			
			_container.Add(_level = new Label("1324") {name = "level"});
			_level.AddToClassList(USS_LEVEL);

			_container.Add(_name = new Label("PLAYER NAME") {name = "name"});
			_name.AddToClassList(USS_NAME);

			var shieldHealthContainer = new VisualElement {name = "shieldhealth-container"};
			_container.Add(shieldHealthContainer);
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
			_container.Add(equipmentContainer);
			equipmentContainer.AddToClassList(USS_EQUIPMENT_CONTAINER);
			{
				equipmentContainer.Add(_equipmentWeapon = new VisualElement {name = "weapon"});
				_equipmentWeapon.AddToClassList(USS_EQUIPMENT);
				_equipmentWeapon.AddToClassList(string.Format(USS_SPRITE_EQUIPMENTCATEGORY, "weapon"));

				equipmentContainer.Add(_equipmentHelmet = new VisualElement {name = "helmet"});
				_equipmentHelmet.AddToClassList(USS_EQUIPMENT);
				_equipmentHelmet.AddToClassList(string.Format(USS_SPRITE_EQUIPMENTCATEGORY, "helmet"));

				equipmentContainer.Add(_equipmentShield = new VisualElement {name = "shield"});
				_equipmentShield.AddToClassList(USS_EQUIPMENT);
				_equipmentShield.AddToClassList(string.Format(USS_SPRITE_EQUIPMENTCATEGORY, "shield"));

				equipmentContainer.Add(_equipmentAmulet = new VisualElement {name = "amulet"});
				_equipmentAmulet.AddToClassList(USS_EQUIPMENT);
				_equipmentAmulet.AddToClassList(string.Format(USS_SPRITE_EQUIPMENTCATEGORY, "amulet"));

				equipmentContainer.Add(_equipmentArmor = new VisualElement {name = "armor"});
				_equipmentArmor.AddToClassList(USS_EQUIPMENT);
				_equipmentArmor.AddToClassList(string.Format(USS_SPRITE_EQUIPMENTCATEGORY, "armor"));
			}

			_damageAnimation = _bg.experimental.animation.Start(0f, 1f, DAMAGE_ANIMATION_DURATION,
				(e, o) => e.style.unityBackgroundImageTintColor = Color.Lerp(DAMAGE_BG, NORMAL_BG, o)).KeepAlive();
			_damageAnimation.Stop();

			_damageAnimationHandle = _bg.schedule.Execute(_damageAnimation.Start);
			_damageAnimationHandle.Pause();

			this.Query().Build().ForEach(e => e.pickingMode = PickingMode.Ignore);
		}

		public void SetTeamColor(Color? color)
		{
			if(!color.HasValue) _teamColor.SetDisplay(false);
			else _teamColor.style.backgroundColor = color.Value;
		}

		public void SetPlayer(PlayerRef player, string playerName, int level, string pfpUrl, Color playerNameColor)
		{
			if (_player == player) return;
			_player = player;

			_name.text = playerName;
			_name.style.color = playerNameColor;
			_level.text = level.ToString();

			if (Application.isPlaying)
			{
				// pfpUrl =
				// 	$"https://mainnetprodflghubstorage.blob.core.windows.net/collections/corpos/{Random.Range(1, 888)}.png";

				if (!string.IsNullOrEmpty(pfpUrl))
				{
					_pfpRequestHandle = MainInstaller.Resolve<IGameServices>().RemoteTextureService.RequestTexture(
						pfpUrl,
						tex =>
						{
							if (_pfp != null && _pfp.panel != null)
							{
								_pfp.style.backgroundImage = new StyleBackground(tex);
							}
						}, null);
				}
				else
				{
					_pfp.style.backgroundImage = StyleKeyword.Null;
				}
			}
		}

		public void UpdateLevel(int might)
		{
			_level.text = might.ToString();
		}

		public void UpdateHealth(float health)
		{
			_healthBar.style.flexGrow = health;
		}

		public void UpdateShield(float shield)
		{
			_shieldBar.style.flexGrow = shield;
		}

		public void UpdateEquipment(Equipment equipment)
		{
			switch (equipment.GetEquipmentGroup())
			{
				case GameIdGroup.Helmet when !_equipmentHelmet.ClassListContains(USS_EQUIPMENT_ACQUIRED):
					_equipmentHelmet.AddToClassList(USS_EQUIPMENT_ACQUIRED);
					_equipmentHelmet.AnimatePing();
					break;
				case GameIdGroup.Weapon when !_equipmentWeapon.ClassListContains(USS_EQUIPMENT_ACQUIRED):
					_equipmentWeapon.AddToClassList(USS_EQUIPMENT_ACQUIRED);
					_equipmentWeapon.AnimatePing();
					break;
				case GameIdGroup.Amulet when !_equipmentAmulet.ClassListContains(USS_EQUIPMENT_ACQUIRED):
					_equipmentAmulet.AddToClassList(USS_EQUIPMENT_ACQUIRED);
					_equipmentAmulet.AnimatePing();
					break;
				case GameIdGroup.Armor when !_equipmentArmor.ClassListContains(USS_EQUIPMENT_ACQUIRED):
					_equipmentArmor.AddToClassList(USS_EQUIPMENT_ACQUIRED);
					_equipmentArmor.AnimatePing();
					break;
				case GameIdGroup.Shield when !_equipmentShield.ClassListContains(USS_EQUIPMENT_ACQUIRED):
					_equipmentShield.AddToClassList(USS_EQUIPMENT_ACQUIRED);
					_equipmentShield.AnimatePing();
					break;
				default:
					FLog.Verbose($"Equipment piece already acquired: {equipment.GameId}");
					break;
			}
		}

		public void SetDead()
		{
			AddToClassList(USS_DEAD);
		}

		public void PingDamage()
		{
			_damageAnimation.Stop();
			_bg.style.unityBackgroundImageTintColor = DAMAGE_BG;
			_damageAnimationHandle.ExecuteLater(1000);
		}

		public new class UxmlFactory : UxmlFactory<SquadMemberElement, UxmlTraits>
		{
		}
	}
}