using System;
using FirstLight.Game.Input;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using Quantum.Commands;
using FirstLight.UiService;
using MoreMountains.NiceVibrations;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for match controls.
	/// </summary>
	public class MatchControlsHudPresenter : UiPresenter, LocalInput.IGameplayActions
	{
		[SerializeField] private SpecialButtonView _specialButton0;
		[SerializeField] private SpecialButtonView _specialButton1;
		[SerializeField] private GameObject[] _disableWhileParachuting;

		private IGameServices _services;
		private LocalInput _localInput;
		private Quantum.Input _quantumInput;
		private EntityRef _entity;


		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_localInput = new LocalInput();
			_localInput.Gameplay.SetCallbacks(this);
		}

		private void Start()
		{
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

			SendSpecialUsedCommand(0, _localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
		}

		/// <inheritdoc />
		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			if (context.ReadValueAsButton() || Math.Abs(context.time - context.startTime) > Mathf.Epsilon)
			{
				return;
			}

			SendSpecialUsedCommand(1, _localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
		}

		private void OnPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			if (callback.HasRespawned)
			{
				return;
			}

			var playerCharacter = callback.Game.Frames.Verified.Get<PlayerCharacter>(callback.Entity);
			_entity = callback.Entity;

			_specialButton0.Init(playerCharacter.Specials[0].SpecialId);
			_specialButton1.Init(playerCharacter.Specials[1].SpecialId);
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
			if (callback.InterimArmourDamage > 0)
			{
				PlayHapticFeedbackForDamage(callback.InterimArmourDamage, callback.MaxInterimArmour);
			}
			else if (callback.HealthDamage > 0)
			{
				PlayHapticFeedbackForDamage(callback.HealthDamage, callback.MaxHealth);
			}
		}

		private void PlayHapticFeedbackForDamage(float damage, float maximumOfRelevantStat)
		{
			var damagePercentOfStat = damage / maximumOfRelevantStat;

			var intensity = Mathf.Lerp(GameConstants.HAPTIC_DAMAGE_INTENSITY_MIN,
			                           GameConstants.HAPTIC_DAMAGE_INTENSITY_MAX, damagePercentOfStat);

			// Sharpness is only used in iOS vibrations
			var sharpness = Mathf.Lerp(GameConstants.HAPTIC_IOS_DAMAGE_SHARPNESS_MIN,
			                           GameConstants.HAPTIC_IOS_DAMAGE_SHARPNESS_MAX, damagePercentOfStat);

			MMVibrationManager.ContinuousHaptic(intensity, sharpness, GameConstants.HAPTIC_DAMAGE_DURATION);
		}

		private void OnWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) callback.Weapon.GameId);

			_localInput.Gameplay.SpecialButton0.Disable();
			_localInput.Gameplay.SpecialButton1.Disable();

			_specialButton0.Init(config.Specials[0]);
			_specialButton1.Init(config.Specials[1]);

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
}