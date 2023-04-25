using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class WeaponDisplayView : IUIView
	{
		private const int BOOMSTICK_INDEX = 1;
		private const int MELEE_INDEX = 0;

		private const string UssSpriteRarity = "sprite-equipmentcard__card-rarity-{0}";
		private const string UssSpriteFaction = "sprite-equipmentcard__card-faction-{0}";

		private const string UssMeleeWeapon = "weapon-display--melee";

		private ImageButton _root;
		private VisualElement _melee;
		private VisualElement _weapon;
		private VisualElement _weaponRarity;
		private VisualElement _weaponIcon;
		private VisualElement _weaponShadow;
		private VisualElement _factionIcon;

		private IGameServices _services;

		public void Attached(VisualElement element)
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_root = (ImageButton) element;
			_melee = element.Q("Melee").Required();
			_weapon = element.Q("Boomstick").Required();
			_weaponRarity = _weapon.Q("WeaponRarityIcon").Required();
			_weaponIcon = _weapon.Q("WeaponIcon").Required();
			_weaponShadow = _weapon.Q("WeaponIconShadow").Required();
			_factionIcon = _weapon.Q("FactionIcon").Required();

			_root.clicked += OnClick;
		}

		public void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponAdded>(OnLocalPlayerWeaponAdded);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(OnLocalPlayerSpawned);
		}

		public void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var pc = callback.Game.Frames.Verified.Get<PlayerCharacter>(callback.Entity);
			SetWeapon(pc.WeaponSlots[BOOMSTICK_INDEX].Weapon);
		}

		private void OnLocalPlayerWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			SetWeapon(callback.Weapon);
		}

		private async void SetWeapon(Equipment weapon)
		{
			if (!weapon.IsValid()) return;

			_weaponRarity.RemoveSpriteClasses();
			_weaponRarity.AddToClassList(string.Format(UssSpriteRarity,
				weapon.Rarity.ToString().Replace("Plus", "").ToLowerInvariant()));

			_factionIcon.RemoveSpriteClasses();
			_factionIcon.AddToClassList(string.Format(UssSpriteFaction, weapon.Faction.ToString().ToLowerInvariant()));

			_weaponIcon.style.backgroundImage = null;
			_weaponShadow.style.backgroundImage = null;

			_weaponIcon.style.backgroundImage = _weaponShadow.style.backgroundImage =
				new StyleBackground(
					await _services.AssetResolverService.RequestAsset<GameId, Sprite>(weapon.GameId, instantiate: false)
				);
		}

		private void OnClick()
		{
			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);
			var pc = f.Get<PlayerCharacter>(data.Entity);
			var nextSlot = pc.CurrentWeaponSlot == MELEE_INDEX ? BOOMSTICK_INDEX : MELEE_INDEX;

			if (pc.WeaponSlots[nextSlot].Weapon.IsValid())
			{
				QuantumRunner.Default.Game.SendCommand(new WeaponSlotSwitchCommand {WeaponSlotIndex = nextSlot});

				if (nextSlot == MELEE_INDEX)
				{
					_melee.BringToFront();
					_root.EnableInClassList(UssMeleeWeapon, true);
				}
				else
				{
					_weapon.BringToFront();
					_root.EnableInClassList(UssMeleeWeapon, false);
				}
			}
		}
	}
}