using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using Quantum.Systems;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Views.UITK
{
	public class WeaponDisplayView : UIView
	{
		private const string USS_GOLDEN_MODIFIER = "weapon-display__rarity-icon--golden";
		private const string USS_MELEE_WEAPON = "weapon-display--melee";
		private const float LOW_AMMO_PERCENTAGE = 0.1f;
		private const float DESATURATION_PERCENTAGE_AMMO_RADIAL_TRACK_ON_BACKGROUND = 0.5f;

		public Gradient OutOfAmmoColors { get; set; }

		private VisualElement _melee;
		private VisualElement _meleeIcon;
		private VisualElement _meleeIconShadow;
		private VisualElement _weapon;
		private VisualElement _weaponRarity;
		private VisualElement _weaponIcon;
		private VisualElement _weaponShadow;
		private VisualElement _switchIcon;
		private RadialProgressElement _ammoProgress;
		private VisualElement _outOfAmmoGlow;
		private Label _ammoLabel;
		private IValueAnimation _ammoLabelAnimation;
		private IValueAnimation _outOfAmmoProgressAnimation;

		private IGameServices _services;
		private IMatchServices _matchServices;

		public event Action<float> OnClick;

		protected override void Attached()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_meleeIconShadow = Element.Q("MeleeIconShadow").Required();
			_meleeIcon = Element.Q("MeleeIcon").Required();
			_melee = Element.Q("Melee").Required();
			_weapon = Element.Q("Boomstick").Required();
			_switchIcon = Element.Q("SwitchIcon").Required();
			_weaponRarity = _weapon.Q("WeaponRarityIcon").Required();
			_weaponIcon = _weapon.Q("WeaponIcon").Required();
			_weaponShadow = _weapon.Q("WeaponIconShadow").Required();
			_ammoLabel = Element.Q<Label>("Ammo").Required();
			_ammoProgress = Element.Q<RadialProgressElement>("AmmoProgress").Required();
			_outOfAmmoGlow = Element.Q<VisualElement>("AmmoProgressBg").Required();
			_ammoProgress.Progress = 0f;

			((ImageButton) Element).clicked += () =>
			{
				// Both have to be sent so the input system resets the click state
				OnClick?.Invoke(1.0f);
				OnClick?.Invoke(0.0f);
			};
		}

		public override void OnScreenOpen(bool reload)
		{
			QuantumEvent.SubscribeManual<EventOnPlayerWeaponAdded>(OnPlayerWeaponAdded);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponChanged>(OnLocalPlayerWeaponChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerAmmoChanged>(OnPlayerAmmoChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerKnockedOut>(OnPlayerKnockedOut);
			QuantumEvent.SubscribeManual<EventOnPlayerRevived>(OnPlayerRevived);
		}

		private void OnPlayerRevived(EventOnPlayerRevived callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;
			UpdateFromLatestVerifiedFrame();
		}

		private void OnPlayerKnockedOut(EventOnPlayerKnockedOut callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;
			UpdateFromLatestVerifiedFrame();
		}

		public override void OnScreenClose()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		public void UpdateFromLatestVerifiedFrame()
		{
			var playerEntity = QuantumRunner.Default.Game.GetLocalPlayerEntityRef();
			var f = QuantumRunner.Default.Game.Frames.Verified;

			Element.SetDisplay(!ReviveSystem.IsKnockedOut(f, playerEntity));
			if (!f.TryGet<PlayerCharacter>(playerEntity, out var pc))
			{
				return;
			}

			var loadout = PlayerLoadout.GetLoadout(f, playerEntity);
			var item = _services.CollectionService.GetCosmeticForGroup(loadout.Cosmetics, GameIdGroup.MeleeSkin);
			SetMeeleSkin(item).Forget();

			SetWeapon(pc.WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon).Forget();
			SetSlot(pc.CurrentWeaponSlot);
			_ammoLabel.text = "0";
			UpdateAmmo(f, playerEntity);
		}

		private async UniTaskVoid SetMeeleSkin(ItemData skin)
		{
			var sprite = _services.CollectionService.LoadCollectionItemSprite(skin);
			await UIUtils.SetSprite(sprite, _meleeIcon, _meleeIconShadow);
			_meleeIcon.RemoveModifiers();
			_meleeIconShadow.RemoveModifiers();
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			UpdateFromLatestVerifiedFrame();
		}

		private void OnPlayerWeaponAdded(EventOnPlayerWeaponAdded callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;

			SetWeapon(callback.Weapon).Forget();
		}

		private void OnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetSlot(callback.Slot);
			// need to update ammo because there is changes of the no ammo effect
			UpdateAmmo(callback.Game.Frames.Verified, callback.Entity);
		}

		private void OnPlayerAmmoChanged(EventOnPlayerAmmoChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			UpdateAmmo(callback.Game.Frames.Verified, callback.Entity);
		}

		private unsafe void UpdateAmmo(Frame f, EntityRef entity)
		{
			if (!f.Exists(entity)) return;
			var pc = f.Unsafe.GetPointer<PlayerCharacter>(entity);
			var weapon = pc->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY];
			var isPrimarySelected = pc->CurrentWeaponSlot == Constants.WEAPON_INDEX_PRIMARY;
			if (!weapon.Weapon.IsValid())
			{
				StopOutOfAmmoAnimation(true);
				SetLowAmmo(false);
				return;
			}

			var ammoPct = AmmoUtils.GetCurrentAmmoPercentage(f, entity).AsFloat;
			var currentAmmo = AmmoUtils.GetCurrentAmmoForGivenWeapon(f, entity, weapon);
			_ammoProgress.Progress = ammoPct;
			if (currentAmmo == 0)
			{
				OutOfAmmo(isPrimarySelected);
			}
			else
			{
				StopOutOfAmmoAnimation(true);
			}

			// Low ammo glow effect 
			SetLowAmmo(IsLowAmmo(ammoPct));

			//TODO: this callback does not apply when you first spawn in for some reason, even though it should
			_ammoLabel.text = currentAmmo.ToString();

			if (_ammoLabelAnimation == null || !_ammoLabelAnimation.isRunning)
			{
				_ammoLabelAnimation = _ammoLabel.AnimatePing(2f, 10);
			}
		}

		private bool IsLowAmmo(float percentage)
		{
			return percentage <= LOW_AMMO_PERCENTAGE;
		}

		public void SetLowAmmo(bool value)
		{
			_outOfAmmoGlow.SetVisibility(value);
		}

		private void StopOutOfAmmoAnimation(bool resetColor = false)
		{
			if (_outOfAmmoProgressAnimation != null)
			{
				_outOfAmmoProgressAnimation.Stop();
				_outOfAmmoProgressAnimation = null;
			}

			if (resetColor)
			{
				_ammoProgress.ParseStyles();
			}
		}

		private void OutOfAmmo(bool isPrimarySelected)
		{
			StopOutOfAmmoAnimation();
			_ammoProgress.Progress = 0f;
			var animation = _ammoProgress.experimental.animation.Start(0, 1, 3000, (element, f) =>
			{
				if (_outOfAmmoProgressAnimation == null)
				{
					return;
				}

				var color = OutOfAmmoColors.Evaluate(f);
				if (!isPrimarySelected)
				{
					var multiplier = DESATURATION_PERCENTAGE_AMMO_RADIAL_TRACK_ON_BACKGROUND;
					color = new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
				}

				((RadialProgressElement) element).TrackColor = color;
			});
			animation.OnCompleted(() =>
			{
				// if there is another animation running stop the loop 
				if (_outOfAmmoProgressAnimation != animation)
				{
					return;
				}

				OutOfAmmo(isPrimarySelected);
			});
			_outOfAmmoProgressAnimation = animation;
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

		private async UniTaskVoid SetWeapon(Equipment weapon)
		{
			_weaponRarity.RemoveSpriteClasses();
			_weaponIcon.style.backgroundImage = null;
			_weaponShadow.style.backgroundImage = null;

			_ammoLabel.SetVisibility(weapon.IsValid());
			_switchIcon.SetVisibility(weapon.IsValid());
			if (!weapon.IsValid())
			{
				_weaponRarity.RemoveModifiers();
				return;
			}

			_weaponRarity.EnableInClassList(USS_GOLDEN_MODIFIER, weapon.Material == EquipmentMaterial.Golden);
			var weaponSprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(weapon.GameId, instantiate: false);
			_weaponIcon.style.backgroundImage = _weaponShadow.style.backgroundImage = new StyleBackground(weaponSprite);
		}
	}
}