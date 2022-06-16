using System;
using System.Collections.Generic;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using Quantum.Commands;
using FirstLight.UiService;
using MoreMountains.NiceVibrations;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for match controls.
	/// </summary>
	public class MatchControlsHudPresenter : UiPresenter, LocalInput.IGameplayActions
	{
		[SerializeField, Required] private SpecialButtonView _specialButton0;
		[SerializeField, Required] private SpecialButtonView _specialButton1;
		[SerializeField] private GameObject[] _disableWhileParachuting;
		
		private IGameServices _services;
		private LocalInput _localInput;
		private Quantum.Input _quantumInput;
		private int _currentWeaponSlot;
		private SpecialsChargesManager _specialCharges;
		
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_specialCharges = new SpecialsChargesManager();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_localInput = new LocalInput();

			_currentWeaponSlot = 0;

			_localInput.Gameplay.SetCallbacks(this);

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLanded);
			QuantumEvent.Subscribe<EventOnLocalPlayerDamaged>(this, OnLocalPlayerDamaged);
		}

		private void OnDestroy()
		{
			_localInput?.Dispose();
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		protected override void OnOpened()
		{
			_localInput.Enable();
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponAdded>(this, OnWeaponAdded);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnWeaponChanged);
			QuantumCallback.Subscribe<CallbackPollInput>(this, PollInput);
		}

		protected override void OnClosed()
		{
			_localInput.Disable();
			QuantumCallback.UnsubscribeListener(this);
		}

		/// <inheritdoc />
		public void OnMove(InputAction.CallbackContext context)
		{
			_quantumInput.Direction = context.ReadValue<Vector2>().ToFPVector2();
		}

		/// <inheritdoc />
		public void OnAim(InputAction.CallbackContext context)
		{
			_quantumInput.AimingDirection = context.ReadValue<Vector2>().ToFPVector2();
		}

		/// <inheritdoc />
		public void OnSpecialAim(InputAction.CallbackContext context)
		{
			// Do Nothing. Handled on the Button command
		}

		/// <inheritdoc />
		public void OnAimButton(InputAction.CallbackContext context)
		{
			_quantumInput.AimButtonState =
				context.ReadValueAsButton() ? Quantum.Input.DownState : Quantum.Input.ReleaseState;
		}

		/// <inheritdoc />
		public void OnSpecialButton0(InputAction.CallbackContext context)
		{
			// Only triggers the input if the button is released or it was not disabled (ex: weapon replaced)
			if (context.ReadValueAsButton() || Math.Abs(context.time - context.startTime) > Mathf.Epsilon)
			{
				return;
			}

			_specialCharges.SpendCharge(0, _currentWeaponSlot);
			SendSpecialUsedCommand(0, _localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
		}

		/// <inheritdoc />
		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			if (context.ReadValueAsButton() || Math.Abs(context.time - context.startTime) > Mathf.Epsilon)
			{
				return;
			}
			_specialCharges.SpendCharge(1,_currentWeaponSlot);
			SendSpecialUsedCommand(1, _localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
		}

		private void OnPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			if (callback.HasRespawned)
			{
				// For when we respawn in deathmatch, we get all our weapon slot charges back
				_specialCharges.ResetAllCharges();
				return;
			}

			var playerCharacter = callback.Game.Frames.Verified.Get<PlayerCharacter>(callback.Entity);
			_currentWeaponSlot = 0;
			_specialButton0.Init(playerCharacter.Specials[0].SpecialId, _specialCharges.HasCharge(0, _currentWeaponSlot));
			_specialButton1.Init(playerCharacter.Specials[1].SpecialId, _specialCharges.HasCharge(1, _currentWeaponSlot));
		}

		private void OnLocalPlayerSkydiveDrop(EventOnLocalPlayerSkydiveDrop callback)
		{
			_localInput.Gameplay.SpecialButton0.Disable();
			_localInput.Gameplay.SpecialButton1.Disable();
			_localInput.Gameplay.Aim.Disable();

			foreach (var go in _disableWhileParachuting)
			{
				go.SetActive(false);
			}
		}

		private void OnLocalPlayerSkydiveLanded(EventOnLocalPlayerSkydiveLand callback)
		{
			_localInput.Gameplay.SpecialButton0.Enable();
			_localInput.Gameplay.SpecialButton1.Enable();
			_localInput.Gameplay.Aim.Enable();

			foreach (var go in _disableWhileParachuting)
			{
				go.SetActive(true);
			}
		}

		private void OnLocalPlayerDamaged(EventOnLocalPlayerDamaged callback)
		{
			if (callback.ShieldDamage > 0)
			{
				PlayHapticFeedbackForDamage(callback.ShieldDamage, callback.ShieldCapacity);
			}
			else if (callback.HealthDamage > 0)
			{
				PlayHapticFeedbackForDamage(callback.HealthDamage, callback.MaxHealth);
			}
		}

		private void PlayHapticFeedbackForDamage(float damage, float maximumOfRelevantStat)
		{
			var damagePercentOfStat = damage / maximumOfRelevantStat;

			var intensity = Mathf.Lerp(GameConstants.Haptics.DAMAGE_INTENSITY_MIN,
			                           GameConstants.Haptics.DAMAGE_INTENSITY_MAX, damagePercentOfStat);

			// Sharpness is only used in iOS vibrations
			var sharpness = Mathf.Lerp(GameConstants.Haptics.IOS_DAMAGE_SHARPNESS_MIN,
			                           GameConstants.Haptics.IOS_DAMAGE_SHARPNESS_MAX, damagePercentOfStat);

			MMVibrationManager.ContinuousHaptic(intensity, sharpness, GameConstants.Haptics.DAMAGE_DURATION);
		}

		private void OnWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			// If in DeathMatch we will let the player get his special's charges back when he picks up another weapon 
			if (_gameDataProvider.AppDataProvider.SelectedGameMode.Value == GameMode.Deathmatch)
			{
				_specialCharges.ResetCharges(callback.WeaponSlotNumber);
			}
		}
		
		private void OnWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) callback.Weapon.GameId);

			_currentWeaponSlot = callback.Slot;
			
			_localInput.Gameplay.SpecialButton0.Disable();
			_localInput.Gameplay.SpecialButton1.Disable();

			_specialButton0.Init(config.Specials[0], _specialCharges.HasCharge(0, _currentWeaponSlot));
			_specialButton1.Init(config.Specials[1], _specialCharges.HasCharge(1, _currentWeaponSlot));

			_localInput.Gameplay.SpecialButton0.Enable();
			_localInput.Gameplay.SpecialButton1.Enable();
		}

		private void PollInput(CallbackPollInput callback)
		{
			callback.SetInput(_quantumInput, DeterministicInputFlags.Repeatable);
		}

		private void SendSpecialUsedCommand(int specialIndex, Vector2 aimDirection)
		{
			var command = new SpecialUsedCommand
			{
				SpecialIndex = specialIndex,
				AimInput = aimDirection.ToFPVector2(),
			};

			QuantumRunner.Default.Game.SendCommand(command);
		}
	}

	internal class SpecialsChargesManager
	{
		private readonly Dictionary<int, bool>[] _weaponSlotChargesUsedSpecial = { new(), new() };
		
		/// <summary>
		/// Spends the charge of that special for that weapon slot
		/// </summary>
		public void SpendCharge(int specialIndex, int weaponSlotIndex)
		{
			_weaponSlotChargesUsedSpecial[specialIndex][weaponSlotIndex] = false;
		}

		/// <summary>
		/// Indicates if a certain weapon slot still has a certain special button's charge to use
		/// </summary>
		public bool HasCharge(int specialIndex, int weaponSlotIndex)
		{
			var value =  !_weaponSlotChargesUsedSpecial[specialIndex].ContainsKey(weaponSlotIndex)
			       || _weaponSlotChargesUsedSpecial[specialIndex][weaponSlotIndex] == true;

			return value;
		}
		
		/// <summary>
		/// Resets charges for all weapon slots
		/// </summary>
		public void ResetAllCharges()
		{
			foreach (var specialSlotCharges in _weaponSlotChargesUsedSpecial)
			{
				specialSlotCharges.Clear();
			}
		}
		
		/// <summary>
		/// Resets charges for a weapon slot
		/// </summary>
		public void ResetCharges(int weaponSlot)
		{
			foreach (var specialSlotCharges in _weaponSlotChargesUsedSpecial)
			{
				specialSlotCharges[weaponSlot] = false;
			}
		}
	}
}