using System;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
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
	public class MatchControlsHudPresenter : UiPresenter, LocalInput.IGameplayActions
	{
		[SerializeField, Required] private SpecialButtonView _specialButton0;
		[SerializeField, Required] private SpecialButtonView _specialButton1;
		[SerializeField] private GameObject[] _disableWhileParachuting;
		[SerializeField] private Button[] _weaponSlotButtons;
		[SerializeField, Required] private GameObject _weaponSlotsHolder;
		
		private IGameServices _services;
		private LocalInput _localInput;
		private Quantum.Input _quantumInput;
		private int _currentWeaponSlot;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_localInput = new LocalInput();

			_currentWeaponSlot = 0;

			_localInput.Gameplay.SetCallbacks(this);
			
			_weaponSlotButtons[0].onClick.AddListener(() => OnWeaponSlotClicked(0));
			_weaponSlotButtons[1].onClick.AddListener(() => OnWeaponSlotClicked(1));
			_weaponSlotButtons[2].onClick.AddListener(() => OnWeaponSlotClicked(2));

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
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
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var isBattleRoyale = frame.Context.MapConfig.GameMode == GameMode.BattleRoyale;
			
			_localInput.Enable();
			
			_weaponSlotsHolder.gameObject.SetActive(isBattleRoyale);
			
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
			// TODO: Use mouse position and clicks
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
		
		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync || _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}
			
			var localPlayer = msg.Game.GetLocalPlayerData(false, out var f);

			if (!localPlayer.Entity.IsAlive(f) || !f.TryGet<PlayerCharacter>(localPlayer.Entity, out var playerCharacter))
			{
				_localInput.Gameplay.Disable();
				return;
			}
			
			_currentWeaponSlot = playerCharacter.CurrentWeaponSlot;
			
			SetupSpecials(f, localPlayer.Entity);

			if (f.Get<AIBlackboardComponent>(localPlayer.Entity).GetBoolean(f, Constants.IsSkydiving))
			{
				OnLocalPlayerSkydiveDrop(null);
			}
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var f = callback.Game.Frames.Predicted;
			
			_currentWeaponSlot = 0;

			SetupSpecials(f, callback.Entity);
		}

		private void OnWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			var f = callback.Game.Frames.Predicted;

			_currentWeaponSlot = callback.Slot;
			
			SetupSpecials(f, callback.Entity);
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
		
		private void OnWeaponSlotClicked(int weaponSlotIndex)
		{
			var command = new WeaponSlotSwitchCommand()
			{
				WeaponSlotIndex = weaponSlotIndex
			};
			
			QuantumRunner.Default.Game.SendCommand(command);
		}

		private void SetupSpecials(Frame f, EntityRef entity)
		{
			if (!f.TryGet<PlayerCharacter>(entity, out var playerCharacter))
			{
				_localInput.Gameplay.Disable();
				return;
			}

			var weaponSlot = playerCharacter.WeaponSlots[_currentWeaponSlot];
			
			if (weaponSlot.Special1.IsValid)
			{
				_localInput.Gameplay.SpecialButton1.Enable();
				_specialButton0.Init(f, weaponSlot.Special1, weaponSlot.Special1Charges > 0);
			}
			else
			{
				_localInput.Gameplay.SpecialButton0.Disable();
				_specialButton0.gameObject.SetActive(false);
			}
			if (weaponSlot.Special2.IsValid)
			{
				_localInput.Gameplay.SpecialButton0.Enable();
				_specialButton1.Init(f, weaponSlot.Special2, weaponSlot.Special2Charges > 0);
			}
			else
			{
				_localInput.Gameplay.SpecialButton1.Disable();
				_specialButton1.gameObject.SetActive(false);
			}
		}
	}
}