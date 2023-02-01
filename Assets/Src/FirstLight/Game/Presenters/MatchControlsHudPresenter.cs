using System.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.Input;
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
using FirstLight.Game.Views;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for match controls.
	/// </summary>
	public class MatchControlsHudPresenter : UiPresenter, LocalInput.IGameplayActions
	{
		[SerializeField, Required] private WeaponSlotView[] _slots;
		[SerializeField, Required] private SpecialButtonView[] _specialButtons;
		[SerializeField, Required] private GameObject[] _disableWhileParachuting;
		[SerializeField, Required] private GameObject _weaponSlotsHolder;
		[SerializeField, Required] private GameObject _gunSwitchButton;
		[SerializeField, Required] private UiButtonView _pingButton;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private Quantum.Input _quantumInput;
		private LocalPlayerIndicatorContainerView _indicatorContainerView;

		private bool _allowPing = true;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_indicatorContainerView = new LocalPlayerIndicatorContainerView(_services);

			for (var i = 0; i < _slots.Length; i++)
			{
				_slots[i].Init(i);
			}

			_weaponSlotsHolder.gameObject.SetActive(false);
			_specialButtons[0].OnCancelEnter
				.AddListener(() => _indicatorContainerView.GetIndicator(0)?.SetVisualState(false));
			_specialButtons[0].OnCancelExit
				.AddListener(() => _indicatorContainerView.GetIndicator(0)?.SetVisualState(true));
			_specialButtons[1].OnCancelEnter
				.AddListener(() => _indicatorContainerView.GetIndicator(1)?.SetVisualState(false));
			_specialButtons[1].OnCancelExit
				.AddListener(() => _indicatorContainerView.GetIndicator(1)?.SetVisualState(true));

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnMatchSimulationStartedMessage);
			QuantumEvent.Subscribe<EventOnPlayerAttackHit>(this, OnPlayerAttackHit);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnPlayerKill);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLanded);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpecialUsed>(this, OnEventOnLocalPlayerSpecialUsed);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnWeaponChanged);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponAdded>(this, OnLocalPlayerWeaponAdded);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView);
			QuantumCallback.Subscribe<CallbackPollInput>(this, PollInput);

			_pingButton.gameObject.SetActive(FeatureFlags.SQUAD_PINGS);
		}

		private void OnDestroy()
		{
			_indicatorContainerView?.Dispose();
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void OnOpened()
		{
			_services.PlayerInputService.EnableInput();
		}

		protected override Task OnClosed()
		{
			_services.PlayerInputService.DisableInput();
			return Task.CompletedTask;
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
			var input = _services.PlayerInputService.Input.Gameplay;

			if (input.SpecialButton0.IsPressed())
			{
				_indicatorContainerView.GetIndicator(0).SetTransformState(input.SpecialAim.ReadValue<Vector2>());
			}
			else if (input.SpecialButton1.IsPressed())
			{
				_indicatorContainerView.GetIndicator(1).SetTransformState(input.SpecialAim.ReadValue<Vector2>());
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
			OnSpecialButtonUsed(context, 0);
		}

		/// <inheritdoc />
		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			OnSpecialButtonUsed(context, 1);
		}

		/// <inheritdoc />
		public void OnCancelButton(InputAction.CallbackContext context)
		{
			// TODO: When we decide to officially support gamepads, add dedicated Cancel functionality button.
			// TODO: At this point, input should be conditional, and this code should not run for touch input.
		}

		public void OnSwitchWeaponButton(InputAction.CallbackContext context)
		{
			if (!context.ReadValueAsButton()) return;

			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);

			// Check if there is a point in switching or not. Avoid extra commands to save network message traffic $$$
			if (!f.TryGet<PlayerCharacter>(data.Entity, out var pc))
			{
				return;
			}

			int slotIndexToSwitch;
			if (pc.CurrentWeaponSlot != 1 && pc.WeaponSlots[1].Weapon.IsValid())
			{
				slotIndexToSwitch = 1;
			}
			else if (pc.CurrentWeaponSlot != 2 && pc.WeaponSlots[2].Weapon.IsValid())
			{
				slotIndexToSwitch = 2;
			}
			else if (pc.CurrentWeaponSlot != 0)
			{
				slotIndexToSwitch = 0;
			}
			else
			{
				return;
			}

			QuantumRunner.Default.Game.SendCommand(new WeaponSlotSwitchCommand {WeaponSlotIndex = slotIndexToSwitch});
		}

		public void OnTeamPositionPing(InputAction.CallbackContext context)
		{
			if (!_allowPing ||
				(context.control.device is not OnScreenControlsDevice && context.ReadValueAsButton())) return;

			var command = new TeamPositionPingCommand();

			if (context.control.device is OnScreenControlsDevice || !context.ReadValueAsButton())
			{
				// OnScreen drag & drop
				var screenPosition = context.ReadValue<Vector2>();
				var ray = Camera.main.ScreenPointToRay(screenPosition);
				if (Physics.Raycast(ray, out var hitInfo))
				{
					// TODO: Add types of pings here
					command.Position = hitInfo.point.ToFPVector3();
					command.Type = TeamPingType.General;
				}
				else
				{
					return;
				}
			}
			else
			{
				// Controller
				var player = _matchServices.SpectateService.SpectatedPlayer.Value;
				command.Position = player.Transform.position.ToFPVector3();
				command.Type = TeamPingType.General;
			}

			QuantumRunner.Default.Game.SendCommand(command);

			_allowPing = false;
			DOVirtual.DelayedCall(2f, () => _allowPing = true);
		}

		private void OnSpecialButtonUsed(InputAction.CallbackContext context, int specialIndex)
		{
			var specialButton = _specialButtons[specialIndex];

			if (specialButton.SpecialId == GameId.Random || context.performed)
			{
				return;
			}

			var indicator = _indicatorContainerView.GetIndicator(specialIndex);

			if (context.started)
			{
				indicator.SetVisualState(true);
				indicator.SetTransformState(Vector2.zero);
				return;
			}

			indicator.SetVisualState(false);

			if (context.control.device is OnScreenControlsDevice && !specialButton.DraggingValidPosition())
			{
				return;
			}

			var aim = _services.PlayerInputService.Input.Gameplay.SpecialAim.ReadValue<Vector2>();

			SendSpecialUsedCommand(specialIndex, aim);
		}

		private unsafe void SendSpecialUsedCommand(int specialIndex, Vector2 aimDirection)
		{
			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);

			// Check if there is a weapon equipped in the slot. Avoid extra commands to save network message traffic $$$
			if (!f.TryGet<PlayerCharacter>(data.Entity, out var playerCharacter) ||
				!playerCharacter.WeaponSlot->Specials[specialIndex].IsUsable(f))
			{
				return;
			}

			var command = new SpecialUsedCommand
			{
				SpecialIndex = specialIndex,
				AimInput = aimDirection.ToFPVector2(),
			};

			QuantumRunner.Default.Game.SendCommand(command);
		}

		private unsafe void Init(Frame f, EntityRef entity)
		{
			var playerView = _matchServices.EntityViewUpdaterService.GetManualView(entity);
			var playerCharacter = f.Get<PlayerCharacter>(entity);
			var isSingleMode = f.Context.GameModeConfig.SingleSlotMode;
			var isHammerTime = f.Context.TryGetMutatorByType(MutatorType.HammerTime, out _);
			for (var i = 0; i < _slots.Length; i++)
			{
				var showSlot = isHammerTime
					? i == Constants.WEAPON_INDEX_DEFAULT
					: (!isSingleMode || i != Constants.WEAPON_INDEX_SECONDARY);
				_slots[i].gameObject.SetActive(showSlot);
			}

			_weaponSlotsHolder.SetActive(f.Context.GameModeConfig.ShowWeaponSlots);
			_services.PlayerInputService.Input.Gameplay.SetCallbacks(this);
			_indicatorContainerView.Init(playerView);
			_indicatorContainerView.SetupWeaponInfo(f, playerCharacter.CurrentWeapon.GameId);

			SetupSpecialsInput(f.Time, *playerCharacter.WeaponSlot, playerView);
			SetupGunSwitchButton(playerCharacter.WeaponSlots);
			InitSlotsView(playerCharacter);
		}

		private void OnUpdateView(CallbackUpdateView callback)
		{
			_indicatorContainerView.OnUpdate(callback.Game.Frames.Predicted);
		}

		private void OnMatchSimulationStartedMessage(MatchSimulationStartedMessage msg)
		{
			_indicatorContainerView.InstantiateAllIndicators();
		}

		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync)
			{
				MMVibrationManager.ContinuousHaptic(GameConstants.Haptics.GAME_START_INTENSITY,
					GameConstants.Haptics.GAME_START_SHARPNESS,
					GameConstants.Haptics.GAME_START_DURATION);
			}

			if (!msg.IsResync || _services.NetworkService.LocalPlayer.IsSpectator())
			{
				return;
			}

			var localPlayer = msg.Game.GetLocalPlayerData(false, out var f);

			if (!localPlayer.Entity.IsAlive(f))
			{
				return;
			}

			Init(f, localPlayer.Entity);

			if (f.Get<AIBlackboardComponent>(localPlayer.Entity).GetBoolean(f, Constants.IsSkydiving))
			{
				OnLocalPlayerSkydiveDrop(null);
			}
			else
			{
				OnLocalPlayerSkydiveLanded(null);
			}
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_weaponSlotsHolder.SetActive(false);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var f = callback.Game.Frames.Predicted;

			if (callback.HasRespawned)
			{
				_weaponSlotsHolder.SetActive(f.Context.GameModeConfig.ShowWeaponSlots);
				InitSlotsView(f.Get<PlayerCharacter>(callback.Entity));
				return;
			}

			Init(f, callback.Entity);
		}

		private void OnWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			var playerView = _matchServices.EntityViewUpdaterService.GetManualView(callback.Entity);

			_indicatorContainerView.SetupWeaponInfo(callback.Game.Frames.Verified, callback.WeaponSlot.Weapon.GameId);
			SetupSpecialsInput(callback.Game.Frames.Verified.Time, callback.WeaponSlot, playerView);

			for (var i = 0; i < _slots.Length; i++)
			{
				_slots[i].SetSelected(i == callback.Slot);
			}
		}

		private void OnLocalPlayerSkydiveDrop(EventOnLocalPlayerSkydiveDrop callback)
		{
			var input = _services.PlayerInputService.Input.Gameplay;

			input.SpecialButton0.Disable();
			input.SpecialButton1.Disable();
			input.Aim.Disable();
			input.AimButton.Disable();

			foreach (var go in _disableWhileParachuting)
			{
				go.SetActive(false);
			}
		}

		private void OnLocalPlayerSkydiveLanded(EventOnLocalPlayerSkydiveLand callback)
		{
			foreach (var go in _disableWhileParachuting)
			{
				go.SetActive(true);
			}

			var input = _services.PlayerInputService.Input.Gameplay;

			for (var i = 0; i < _specialButtons.Length; i++)
			{
				if (_specialButtons[i].SpecialId != GameId.Random)
				{
					input.GetSpecialButton(i).Enable();
				}
			}

			input.Aim.Enable();
			input.AimButton.Enable();
		}

		private void OnPlayerAttackHit(EventOnPlayerAttackHit callback)
		{
			if (!callback.Game.PlayerIsLocal(callback.Player)) return;
			var f = callback.Game.Frames.Predicted;
			if (f.TryGet<Stats>(callback.HitEntity, out var hitEntityStats))
			{
				PlayHapticFeedbackForDamage(callback.TotalDamage,
					hitEntityStats.GetStatData(StatType.Health).StatValue.AsFloat);
			}
		}

		private void OnPlayerKill(EventOnPlayerKilledPlayer callback)
		{
			if (!callback.Game.PlayerIsLocal(callback.PlayerKiller)) return;

			MMVibrationManager.ContinuousHaptic(GameConstants.Haptics.PLAYER_KILL_INTENSITY,
				GameConstants.Haptics.PLAYER_KILL_SHARPNESS,
				GameConstants.Haptics.PLAYER_KILL_DURATION);
		}

		private unsafe void OnEventOnLocalPlayerSpecialUsed(EventOnLocalPlayerSpecialUsed callback)
		{
			var button = _specialButtons[callback.SpecialIndex];
			var inputButton = _services.PlayerInputService.Input.Gameplay.GetSpecialButton(callback.SpecialIndex);
			var frame = callback.Game.Frames.Predicted;

			// Disables the input until the cooldown is off
			if (frame.TryGet<PlayerCharacter>(callback.Entity, out var playerCharacter) &&
				playerCharacter.WeaponSlot->Specials[callback.SpecialIndex].Charges == 1)
			{
				inputButton.Disable();
			}

			button.SpecialUpdate(frame.Time, callback.Special)?.OnComplete(inputButton.Enable);
			button.SpecialUpdate(frame.Time, callback.Special)?.OnComplete(inputButton.Enable);
		}

		private void OnLocalPlayerWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			_slots[callback.WeaponSlotNumber].SetEquipment(callback.Weapon);

			var frame = callback.Game.Frames.Verified;
			var playerCharacter = frame.Get<PlayerCharacter>(callback.Entity);
			SetupGunSwitchButton(playerCharacter.WeaponSlots);
		}

		private void InitSlotsView(PlayerCharacter playerCharacter)
		{
			for (var i = 0; i < _slots.Length; i++)
			{
				_slots[i].SetEquipment(playerCharacter.WeaponSlots[i].Weapon);
				_slots[i].SetSelected(i == playerCharacter.CurrentWeaponSlot);
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

		private void SetupGunSwitchButton(FixedArray<WeaponSlot> weaponSlots)
		{
			_gunSwitchButton.SetActive(weaponSlots[1].Weapon.IsValid() || weaponSlots[2].Weapon.IsValid());
		}

		private void SetupSpecialsInput(FP currentTime, WeaponSlot weaponSlot, EntityView playerView)
		{
			for (var i = 0; i < weaponSlot.Specials.Length; i++)
			{
				var special = weaponSlot.Specials[i];
				var inputButton = _services.PlayerInputService.Input.Gameplay.GetSpecialButton(i);

				_indicatorContainerView.SetupIndicator(i, weaponSlot.Specials[i].SpecialId, playerView);
				_specialButtons[i].Init(special.SpecialId);

				if (special.IsValid)
				{
					inputButton.Enable();
					_specialButtons[i].SpecialUpdate(currentTime, special)?.OnComplete(inputButton.Enable);
				}
				else
				{
					inputButton.Disable();
				}
			}
		}
	}
}