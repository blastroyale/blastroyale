using System;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Match;
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
using Object = UnityEngine.Object;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for match controls.
	/// </summary>
	public unsafe class MatchControlsHudPresenter : UiPresenter, LocalInput.IGameplayActions
	{
		[SerializeField, Required] private SpecialButtonView _specialButton0;
		[SerializeField, Required] private SpecialButtonView _specialButton1;
		[SerializeField] private GameObject[] _disableWhileParachuting;
		[SerializeField] private Button[] _weaponSlotButtons;
		[SerializeField, Required] private GameObject _weaponSlotsHolder;
		
		private IGameServices _services;
		private IMatchServices _matchServices;
		private LocalInput _localInput;
		private Quantum.Input _quantumInput;
		private PlayerIndicatorContainerView _indicatorContainerView;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_localInput = new LocalInput();
			_indicatorContainerView = new PlayerIndicatorContainerView(_services);

			_weaponSlotsHolder.gameObject.SetActive(false);
			_weaponSlotButtons[0].onClick.AddListener(() => OnWeaponSlotClicked(0));
			_weaponSlotButtons[1].onClick.AddListener(() => OnWeaponSlotClicked(1));
			_weaponSlotButtons[2].onClick.AddListener(() => OnWeaponSlotClicked(2));

			QuantumCallback.Subscribe<CallbackGameResynced>(this, OnGameResync);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLanded);
			QuantumEvent.Subscribe<EventOnLocalPlayerDamaged>(this, OnLocalPlayerDamaged);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponChanged>(this, OnWeaponChanged);
		}

		private void OnDestroy()
		{
			_indicatorContainerView?.Dispose();
			_localInput?.Dispose();
		}

		protected override void OnOpened()
		{
			_localInput.Enable();
			
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView);
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
			var direction = context.ReadValue<Vector2>();

			_quantumInput.Direction = direction.ToFPVector2();

			_indicatorContainerView.OnMoveUpdate(direction, _quantumInput.IsMoveButtonDown);
		}

		/// <inheritdoc />
		public void OnAim(InputAction.CallbackContext context)
		{
			// TODO: Had mouse input position and clicks
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
			}
			// Only triggers the input if the button is released or it was not disabled (ex: weapon replaced)
			else if(Math.Abs(context.time - context.startTime) > Mathf.Epsilon)
			{
				indicator.SetVisualState(false);
				SendSpecialUsedCommand(0, _localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
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
			}
			// Only triggers the input if the button is released or it was not disabled (ex: weapon replaced)
			else if(Math.Abs(context.time - context.startTime) > Mathf.Epsilon)
			{
				indicator.SetVisualState(false);
				SendSpecialUsedCommand(1, _localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
			}
		}
		
		private void Init(Frame f, EntityRef entity)
		{
			var playerView = _matchServices.EntityViewUpdaterService.GetManualView(entity);
			var playerCharacter = f.Get<PlayerCharacter>(entity);
			
			_weaponSlotsHolder.SetActive(f.Context.MapConfig.GameMode == GameMode.BattleRoyale);
			_localInput.Gameplay.SetCallbacks(this);
			_indicatorContainerView.Init(playerView);
			_indicatorContainerView.SetupWeaponInfo(playerCharacter.WeaponSlot, playerView);
			SetupSpecialsInput(f.Time, playerCharacter.WeaponSlot);
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
			
			_indicatorContainerView.SetupWeaponInfo(callback.WeaponSlot, playerView);
			SetupSpecialsInput(callback.Game.Frames.Predicted.Time, callback.WeaponSlot);
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
			var command = new SpecialUsedCommand
			{
				SpecialIndex = specialIndex,
				AimInput = aimDirection.ToFPVector2(),
			};

			QuantumRunner.Default.Game.SendCommand(command);
		}
		
		private void OnWeaponSlotClicked(int weaponSlotIndex)
		{
			var command = new WeaponSlotSwitchCommand()
			{
				WeaponSlotIndex = weaponSlotIndex
			};
			
			QuantumRunner.Default.Game.SendCommand(command);
		}

		private void SetupSpecialsInput(FP currentTime, WeaponSlot weaponSlot)
		{
			if (weaponSlot.Special1.IsValid)
			{
				_localInput.Gameplay.SpecialButton1.Enable();
				_specialButton0.Init(currentTime, weaponSlot.Special1, weaponSlot.Special1Charges > 0);
			}
			else
			{
				_localInput.Gameplay.SpecialButton0.Disable();
				_specialButton0.gameObject.SetActive(false);
			}
			if (weaponSlot.Special2.IsValid)
			{
				_localInput.Gameplay.SpecialButton0.Enable();
				_specialButton1.Init(currentTime, weaponSlot.Special2, weaponSlot.Special2Charges > 0);
			}
			else
			{
				_localInput.Gameplay.SpecialButton1.Disable();
				_specialButton1.gameObject.SetActive(false);
			}
		}
	}
}