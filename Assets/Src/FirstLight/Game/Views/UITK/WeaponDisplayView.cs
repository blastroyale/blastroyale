using System;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
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

		public event Action<float> OnClick;

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

			_root.clicked += () =>
			{
				// Both have to be sent so the input system resets the click state
				OnClick?.Invoke(1.0f);
				OnClick?.Invoke(0.0f);
			};
		}

		public void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponAdded>(OnLocalPlayerWeaponAdded);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponChanged>(OnLocalPlayerWeaponChanged);
		}

		public void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var pc = callback.Game.Frames.Verified.Get<PlayerCharacter>(callback.Entity);
			SetWeapon(pc.WeaponSlots[BOOMSTICK_INDEX].Weapon);
			SetSlot(MELEE_INDEX);
		}

		private void OnLocalPlayerWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			SetWeapon(callback.Weapon);
		}

		private void OnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetSlot(callback.Slot);
		}

		private void SetSlot(int slot)
		{
			if (slot == MELEE_INDEX)
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

		private async void SetWeapon(Equipment weapon)
		{
			_weaponRarity.RemoveSpriteClasses();
			_factionIcon.RemoveSpriteClasses();
			_weaponIcon.style.backgroundImage = null;
			_weaponShadow.style.backgroundImage = null;

			if (!weapon.IsValid())
			{
				_weaponRarity.AddToClassList(string.Format(UssSpriteRarity,
					EquipmentRarity.Common.ToString().ToLowerInvariant()));

				return;
			}

			_weaponRarity.AddToClassList(string.Format(UssSpriteRarity,
				weapon.Rarity.ToString().Replace("Plus", "").ToLowerInvariant()));

			_factionIcon.AddToClassList(string.Format(UssSpriteFaction, weapon.Faction.ToString().ToLowerInvariant()));

			_weaponIcon.style.backgroundImage = _weaponShadow.style.backgroundImage =
				new StyleBackground(
					await _services.AssetResolverService.RequestAsset<GameId, Sprite>(weapon.GameId, instantiate: false)
				);
		}
	}
}