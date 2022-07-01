using System;
using System.Collections.Generic;
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
			if (!msg.IsResync)
			{
				return;
			}
			
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];

			if (!localPlayer.Entity.IsAlive(f))
			{
				return;
			}

			var playerCharacter = f.Get<PlayerCharacter>(localPlayer.Entity);
			_currentWeaponSlot = 0;
			var currentWeaponSlot = playerCharacter.WeaponSlots[_currentWeaponSlot];
			
			_specialButton0.Init(currentWeaponSlot.Special1.SpecialId, currentWeaponSlot.Special1Charges > 0);
			_specialButton1.Init(currentWeaponSlot.Special2.SpecialId, currentWeaponSlot.Special2Charges > 0);
		}

		private void OnPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			if (callback.HasRespawned)
			{
				return;
			}

			var playerCharacter = callback.Game.Frames.Verified.Get<PlayerCharacter>(callback.Entity);
			_currentWeaponSlot = 0;
			var currentWeaponSlot = playerCharacter.WeaponSlots[_currentWeaponSlot];
			
			_specialButton0.Init(currentWeaponSlot.Special1.SpecialId, currentWeaponSlot.Special1Charges > 0);
			_specialButton1.Init(currentWeaponSlot.Special2.SpecialId, currentWeaponSlot.Special2Charges > 0);
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

		private void OnWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) callback.Weapon.GameId);
			var playerCharacter = callback.Game.Frames.Verified.Get<PlayerCharacter>(callback.Entity);

			_currentWeaponSlot = callback.Slot;
			
			_localInput.Gameplay.SpecialButton0.Disable();
			_localInput.Gameplay.SpecialButton1.Disable();

			_specialButton0.Init(config.Specials[0], playerCharacter.WeaponSlots[_currentWeaponSlot].Special1Charges > 0);
			_specialButton1.Init(config.Specials[1], playerCharacter.WeaponSlots[_currentWeaponSlot].Special2Charges > 0);

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
		
		private void OnWeaponSlotClicked(int weaponSlotIndex)
		{
			var command = new WeaponSlotSwitchCommand()
			{
				WeaponSlotIndex = weaponSlotIndex
			};
			
			QuantumRunner.Default.Game.SendCommand(command);
		}
	}
}