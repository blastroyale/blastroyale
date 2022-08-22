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
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for match controls.
	/// </summary>
	public unsafe class MatchControlsHudPresenter : UiPresenter, LocalInput.IGameplayActions
	{
		[SerializeField, Required] private SpecialButtonView[] _specialButtons;
		[SerializeField, Required] private CancelJoystickView _cancelJoystick;
		[SerializeField] private GameObject[] _disableWhileParachuting;
		[SerializeField] private Button[] _weaponSlotButtons;
		[SerializeField, Required] private GameObject _weaponSlotsHolder;
		
		private IGameServices _services;
		private IMatchServices _matchServices;
		private Quantum.Input _quantumInput;
		private LocalPlayerIndicatorContainerView _indicatorContainerView;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_indicatorContainerView = new LocalPlayerIndicatorContainerView(_services);

			_cancelJoystick.gameObject.SetActive(false);
			_weaponSlotsHolder.gameObject.SetActive(false);
			_weaponSlotButtons[0].onClick.AddListener(() => OnWeaponSlotClicked(0));
			_weaponSlotButtons[1].onClick.AddListener(() => OnWeaponSlotClicked(1));
			_weaponSlotButtons[2].onClick.AddListener(() => OnWeaponSlotClicked(2));

			QuantumCallback.Subscribe<CallbackGameResynced>(this, OnGameResync);
			QuantumEvent.Subscribe<EventOnPlayerDamaged>(this, OnPlayerDamaged);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLanded);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpecialUsed>(this, OnEventOnLocalPlayerSpecialUsed);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnWeaponChanged);
		}

		private void OnDestroy()
		{
			_indicatorContainerView?.Dispose();
			_services?.PlayerInputService?.DisposeInput();
		}

		protected override void OnOpened()
		{
			_services.PlayerInputService.EnableInput();
			
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView);
			QuantumCallback.Subscribe<CallbackPollInput>(this, PollInput);
		}

		protected override void OnClosed()
		{
			_services.PlayerInputService.DisableInput();
			QuantumCallback.UnsubscribeListener(this);
		}

		/// <inheritdoc />
		public void OnMove(InputAction.CallbackContext context)
		{
			var direction = context.ReadValue<Vector2>();

			_quantumInput.Direction = direction.ToFPVector2();

			_indicatorContainerView.OnMoveUpdate(direction, _quantumInput.IsMoveButtonDown);
		}

		/// <inheritdoc />
		public void OnAim(InputAction.CallbackContext context)
		{
			_quantumInput.AimingDirection = context.ReadValue<Vector2>().ToFPVector2();
		}

		/// <inheritdoc />
		public void OnSpecialAim(InputAction.CallbackContext context)
		{
			if (_localInput.Gameplay.SpecialButton0.IsPressed())
			{
				_indicatorContainerView.GetIndicator(0)
				                       .SetTransformState(_localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
			}
			else if (_localInput.Gameplay.SpecialButton1.IsPressed())
			{
				_indicatorContainerView.GetIndicator(1)
				                       .SetTransformState(_localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
			}
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
			var indicator = _indicatorContainerView.GetIndicator(0);
			
			if (context.ReadValueAsButton())
			{
				indicator.SetVisualState(true);
				indicator.SetTransformState(Vector2.zero);
				_cancelJoystick.gameObject.SetActive(true);
				return;
			}
			
			var aim = _localInput.Gameplay.SpecialAim.ReadValue<Vector2>();
			
			indicator.SetVisualState(false);
			_cancelJoystick.gameObject.SetActive(false);

			// Only triggers the input if the button is released or it was not disabled (ex: weapon replaced)
			if (Math.Abs(context.time - context.startTime) < Mathf.Epsilon && 
			    (aim.sqrMagnitude > 0 || indicator.IndicatorVfxId == IndicatorVfxId.None))
			{
				SendSpecialUsedCommand(0, aim);
			}
		}

		/// <inheritdoc />
		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			var indicator = _indicatorContainerView.GetIndicator(1);
			
			if (context.ReadValueAsButton())
			{
				indicator.SetVisualState(true);
				indicator.SetTransformState(Vector2.zero);
				_cancelJoystick.gameObject.SetActive(true);
				return;
			}
			
			var aim = _localInput.Gameplay.SpecialAim.ReadValue<Vector2>();
			
			indicator.SetVisualState(false);
			_cancelJoystick.gameObject.SetActive(false);
			
			// TODO: Check if im.sqrMagnitude > _specialButton0.size
			
			// Only triggers the input if the button is released or it was not disabled (ex: weapon replaced)
			if (Math.Abs(context.time - context.startTime) < Mathf.Epsilon && 
			    (aim.sqrMagnitude > 0 || indicator.IndicatorVfxId == IndicatorVfxId.None))
			{
				SendSpecialUsedCommand(1, aim);
			}
		}

		public void OnCancelButton(InputAction.CallbackContext context)
		{
			if (context.ReadValueAsButton())
			{
				_localInput.Gameplay.SpecialButton0.Disable();
				_localInput.Gameplay.SpecialButton1.Disable();
				_localInput.Gameplay.AimButton.Disable();
				return;
			}
			_localInput.Gameplay.SpecialButton0.Enable();
			_localInput.Gameplay.SpecialButton1.Enable();
			_localInput.Gameplay.AimButton.Enable();
		}

		private void Init(Frame f, EntityRef entity)
		{
			var playerView = _matchServices.EntityViewUpdaterService.GetManualView(entity);
			var playerCharacter = f.Get<PlayerCharacter>(entity);
			
			_weaponSlotsHolder.SetActive(f.Context.MapConfig.GameMode == GameMode.BattleRoyale);
			_localInput.Gameplay.SetCallbacks(this);
			_indicatorContainerView.Init(playerView);
			_indicatorContainerView.SetupWeaponInfo(playerCharacter.CurrentWeapon.GameId);
			SetupSpecialsInput(f.Time, *playerCharacter.WeaponSlot, playerView);
		}

		private void OnUpdateView(CallbackUpdateView callback)
		{
			_indicatorContainerView.OnUpdate(callback.Game.Frames.Predicted);
		}

		private void OnGameResync(CallbackGameResynced callback)
		{
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}
			
			var localPlayer = callback.Game.GetLocalPlayerData(false, out var f);

			if (!localPlayer.Entity.IsAlive(f))
			{
				return;
			}
			
			Init(f, localPlayer.Entity);

			if (f.Get<AIBlackboardComponent>(localPlayer.Entity).GetBoolean(f, Constants.IsSkydiving))
			{
				OnLocalPlayerSkydiveDrop(null);
			}
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			if (callback.HasRespawned)
			{
				return;
			}

			Init(callback.Game.Frames.Predicted, callback.Entity);
		}

		private void OnWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			var playerView = _matchServices.EntityViewUpdaterService.GetManualView(callback.Entity);
			
			_indicatorContainerView.SetupWeaponInfo(callback.WeaponSlot.Weapon.GameId);
			SetupSpecialsInput(callback.Game.Frames.Predicted.Time, callback.WeaponSlot, playerView);
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
			_localInput.Gameplay.Aim.Enable();

			foreach (var go in _disableWhileParachuting)
			{
				go.SetActive(true);
			}
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;
			
			if (callback.ShieldDamage > 0)
			{
				PlayHapticFeedbackForDamage(callback.ShieldDamage, callback.MaxShield);
			}
			else if (callback.HealthDamage > 0)
			{
				PlayHapticFeedbackForDamage(callback.HealthDamage, callback.MaxHealth);
			}
		}

		private void OnEventOnLocalPlayerSpecialUsed(EventOnLocalPlayerSpecialUsed callback)
		{
			var button = _specialButtons[callback.SpecialIndex];
			var inputButton = _localInput.Gameplay.GetSpecialButton(callback.SpecialIndex);
			var currentTime = callback.Game.Frames.Predicted.Time;
			
			button.SpecialUpdate(currentTime, callback.Special)?.OnComplete(inputButton.Enable);
		}

		private void PollInput(CallbackPollInput callback)
		{
			callback.SetInput(_quantumInput, DeterministicInputFlags.Repeatable);
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

		private void SendSpecialUsedCommand(int specialIndex, Vector2 aimDirection)
		{
			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);
			var special = f.Get<PlayerCharacter>(data.Entity).WeaponSlot->Specials[specialIndex];
			
			// Check if there is a weapon equipped in the slot. Avoid extra commands to save network message traffic $$$
			if (!special.IsUsable(f))
			{
				return;
			}

			// Disables the input until the cooldown is off
			if (special.Charges == 1)
			{
				_localInput.Gameplay.GetSpecialButton(specialIndex).Disable();
			}
			
			var command = new SpecialUsedCommand
			{
				SpecialIndex = specialIndex,
				AimInput = aimDirection.ToFPVector2(),
			};

			QuantumRunner.Default.Game.SendCommand(command);
		}
		
		private void OnWeaponSlotClicked(int weaponSlotIndex)
		{
			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);
			var pc = f.Get<PlayerCharacter>(data.Entity);

			// Check if there is a weapon equipped in the slot. Avoid extra commands to save network message traffic $$$
			if (pc.CurrentWeaponSlot == weaponSlotIndex || !pc.WeaponSlots[weaponSlotIndex].Weapon.IsValid())
			{
				return;
			}
			
			var command = new WeaponSlotSwitchCommand()
			{
				WeaponSlotIndex = weaponSlotIndex
			};
			
			QuantumRunner.Default.Game.SendCommand(command);
		}

		private void SetupSpecialsInput(FP currentTime, WeaponSlot weaponSlot, EntityView playerView)
		{
			for (var i = 0; i < weaponSlot.Specials.Length; i++)
			{
				var special = weaponSlot.Specials[i];
				var inputButton = _localInput.Gameplay.GetSpecialButton(i);
				
				_indicatorContainerView.SetupIndicator(i, weaponSlot.Specials[i].SpecialId, playerView);

				if (special.IsValid)
				{
					inputButton.Enable();
					_specialButtons[i].Init(special.SpecialId);
					_specialButtons[i].SpecialUpdate(currentTime, special)?.OnComplete(inputButton.Enable);
				}
				else
				{
					inputButton.Disable();
					_specialButtons[i].gameObject.SetActive(false);
				}
			}
		}
	}
}