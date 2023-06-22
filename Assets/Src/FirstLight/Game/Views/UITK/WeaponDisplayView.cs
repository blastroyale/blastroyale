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
	public class WeaponDisplayView : UIView
	{

		private const string USS_SPRITE_RARITY = "sprite-equipmentcard__card-rarity-{0}";
		private const string USS_SPRITE_FACTION = "sprite-equipmentcard__card-faction-{0}";
		private const string USS_MELEE_WEAPON = "weapon-display--melee";

		private VisualElement _melee;
		private VisualElement _weapon;
		private VisualElement _weaponRarity;
		private VisualElement _weaponIcon;
		private VisualElement _weaponShadow;
		private VisualElement _factionIcon;
		private VisualElement _switchIcon;
		private RadialProgressElement _ammoProgress;
		private Label _ammoLabel;

		private IGameServices _services;
		private IMatchServices _matchServices;

		public event Action<float> OnClick;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_melee = element.Q("Melee").Required();
			_weapon = element.Q("Boomstick").Required();
			_switchIcon = element.Q("SwitchIcon").Required();
			_weaponRarity = _weapon.Q("WeaponRarityIcon").Required();
			_weaponIcon = _weapon.Q("WeaponIcon").Required();
			_weaponShadow = _weapon.Q("WeaponIconShadow").Required();
			_factionIcon = _weapon.Q("FactionIcon").Required();
			_ammoLabel = element.Q<Label>("Ammo").Required();
			_ammoProgress = element.Q<RadialProgressElement>("AmmoProgress").Required();

			((ImageButton) element).clicked += () =>
			{
				// Both have to be sent so the input system resets the click state
				OnClick?.Invoke(1.0f);
				OnClick?.Invoke(0.0f);
			};
		}

		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponAdded>(OnLocalPlayerWeaponAdded);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponChanged>(OnLocalPlayerWeaponChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerAmmoChanged>(OnPlayerAmmoChanged);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var pc = callback.Game.Frames.Verified.Get<PlayerCharacter>(callback.Entity);
			SetWeapon(pc.WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon);
			SetSlot(Constants.WEAPON_INDEX_DEFAULT);
			_ammoLabel.text = "0";
			UpdateAmmo(callback.Game.Frames.Verified, callback.Entity);
		}

		private void OnLocalPlayerWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			SetWeapon(callback.Weapon);
		}

		private void OnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetSlot(callback.Slot);
		}

		private void OnPlayerAmmoChanged(EventOnPlayerAmmoChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			UpdateAmmo(callback.Game.Frames.Verified, callback.Entity);
		}

		private unsafe void UpdateAmmo(Frame f, EntityRef entity)
		{
			var pc = f.Unsafe.GetPointer<PlayerCharacter>(entity);
			var weapon = pc->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY];
			if (!weapon.Weapon.IsValid())
			{
				return;
			}

			var maxAmmo = AmmoUtils.GetMaxAmmo(f, weapon.Weapon.GameId);
			var currentAmmo = AmmoUtils.GetCurrentAmmoForGivenWeapon(f, entity, weapon);
			
			//TODO: change this to be the infinity symbol or something idk
			// also this callback does not apply when you first spawn in for some reason, even though it should
			_ammoLabel.text = currentAmmo + " / " + maxAmmo;

			_ammoProgress.Progress = stats->CurrentAmmoPercent.AsFloat;
		}

		private void SetSlot(int slot)
		{
			if (slot == Constants.WEAPON_INDEX_DEFAULT)
			{
				_melee.BringToFront();
				Element.EnableInClassList(USS_MELEE_WEAPON, true);
			}
			else
			{
				_weapon.BringToFront();
				Element.EnableInClassList(USS_MELEE_WEAPON, false);
			}
		}

		private async void SetWeapon(Equipment weapon)
		{
			_weaponRarity.RemoveSpriteClasses();
			_factionIcon.RemoveSpriteClasses();
			_weaponIcon.style.backgroundImage = null;
			_weaponShadow.style.backgroundImage = null;

			_ammoLabel.SetVisibility(weapon.IsValid());
			_switchIcon.SetVisibility(weapon.IsValid());
			if (!weapon.IsValid())
			{
				_weaponRarity.AddToClassList(string.Format(USS_SPRITE_RARITY,
					EquipmentRarity.Common.ToString().ToLowerInvariant()));

				return;
			}
			_weaponRarity.AddToClassList(string.Format(USS_SPRITE_RARITY,
				weapon.Rarity.ToString().Replace("Plus", "").ToLowerInvariant()));

			_factionIcon.AddToClassList(string.Format(USS_SPRITE_FACTION,
				weapon.Faction.ToString().ToLowerInvariant()));

			_weaponIcon.style.backgroundImage = _weaponShadow.style.backgroundImage =
				new StyleBackground(
					await _services.AssetResolverService.RequestAsset<GameId, Sprite>(weapon.GameId, instantiate: false)
				);
		}
	}
}
