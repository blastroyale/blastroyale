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
		private EntityRef _localPlayerEntity;
		private QuantumWeaponConfig _weaponConfig;
		private IndicatorVfxId _shootIndicatorId;
		private readonly IIndicator[] _indicators = new IIndicator[(int) IndicatorVfxId.TOTAL];
		private readonly IIndicator[] _specialIndicators = new IIndicator[Constants.MAX_SPECIALS];

		private IIndicator ShootIndicator => _indicators[(int)_shootIndicatorId];
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_localInput = new LocalInput();

			_weaponSlotsHolder.gameObject.SetActive(false);
			InstantiatePlayerIndicators();
			
			_weaponSlotButtons[0].onClick.AddListener(() => OnWeaponSlotClicked(0));
			_weaponSlotButtons[1].onClick.AddListener(() => OnWeaponSlotClicked(1));
			_weaponSlotButtons[2].onClick.AddListener(() => OnWeaponSlotClicked(2));

			QuantumCallback.Subscribe<CallbackGameResynced>(this, OnGameResync);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLanded);
			QuantumEvent.Subscribe<EventOnLocalPlayerDamaged>(this, OnLocalPlayerDamaged);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnWeaponChanged);
			QuantumEvent.Subscribe<EventOnLocalPlayerAmmoEmpty>(this, HandleOnLocalPlayerAmmoEmpty);
		}

		private void OnDestroy()
		{
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
			_quantumInput.Direction = context.ReadValue<Vector2>().ToFPVector2();
			
			_indicators[(int) IndicatorVfxId.Movement].SetTransformState(_quantumInput.Direction.ToUnityVector2());
			_indicators[(int) IndicatorVfxId.Movement].SetVisualState(_quantumInput.IsMoveButtonDown);
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
				_specialIndicators[0].SetTransformState(_localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
			}
			else if (_localInput.Gameplay.SpecialButton1.IsPressed())
			{
				_specialIndicators[1].SetTransformState(_localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
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
			if (context.ReadValueAsButton())
			{
				_specialIndicators[0].SetVisualState(true);
				_specialIndicators[0].SetTransformState(Vector2.zero);
			}
			// Only triggers the input if the button is released or it was not disabled (ex: weapon replaced)
			else if(Math.Abs(context.time - context.startTime) > Mathf.Epsilon)
			{
				_specialIndicators[0].SetVisualState(false);
				SendSpecialUsedCommand(0, _localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
			}
		}

		/// <inheritdoc />
		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			if (context.ReadValueAsButton())
			{
				_specialIndicators[1].SetVisualState(true);
				_specialIndicators[1].SetTransformState(Vector2.zero);
			}
			// Only triggers the input if the button is released or it was not disabled (ex: weapon replaced)
			else if(Math.Abs(context.time - context.startTime) > Mathf.Epsilon)
			{
				_specialIndicators[1].SetVisualState(false);
				SendSpecialUsedCommand(1, _localInput.Gameplay.SpecialAim.ReadValue<Vector2>());
			}
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
		
		private void Init(Frame f, EntityRef entity)
		{
			var playerView = _matchServices.EntityViewUpdaterService.GetManualView(entity);
			var playerCharacter = f.Get<PlayerCharacter>(entity);

			_localPlayerEntity = entity;

			foreach (var indicator in _indicators)
			{
				indicator?.Init(playerView);
			}
			
			_weaponSlotsHolder.SetActive(f.Context.MapConfig.GameMode == GameMode.BattleRoyale);
			_localInput.Gameplay.SetCallbacks(this);
			SetupWeaponInfo(f.Time, playerCharacter.WeaponSlot, playerView);
		}

		private void OnUpdateView(CallbackUpdateView callback)
		{
			var f = callback.Game.Frames.Predicted;
			
			if (!f.Unsafe.TryGetPointer<CharacterController3D>(_localPlayerEntity, out var kcc) ||
			    !f.Unsafe.TryGetPointer<PlayerCharacter>(_localPlayerEntity, out var playerCharacter))
			{
				return;
			}

			var playerInput = f.GetPlayerInput(playerCharacter->Player);

			OnUpdateAim(f, playerInput, playerCharacter, kcc);
		}

		private void OnUpdateAim(Frame f, Quantum.Input* input, PlayerCharacter* playerCharacter, CharacterController3D* kcc)
		{
			var isEmptied = playerCharacter->IsAmmoEmpty(f, _localPlayerEntity);
			var speed = kcc->MaxSpeed * kcc->MaxSpeed;
			var velocity = kcc->Velocity.SqrMagnitude;
			var range = _weaponConfig.AttackRange.AsFloat;
			var minAttackAngle = _weaponConfig.MinAttackAngle;
			var maxAttackAngle = _weaponConfig.MaxAttackAngle;
			var lerp = Mathf.Lerp(minAttackAngle, maxAttackAngle, velocity.AsFloat / speed.AsFloat);
			var angleInRad = maxAttackAngle == minAttackAngle ? maxAttackAngle : lerp;
			
			// We use a formula to calculate the scale of a shooting indicator
			var size = Mathf.Max(0.5f, Mathf.Tan(angleInRad * 0.5f * Mathf.Deg2Rad) * range * 2f);

			// For a melee weapon with a splash damage we use a separate calculation for an indicator
			if (_weaponConfig.IsMeleeWeapon && _weaponConfig.SplashRadius > FP._0)
			{
				range += _weaponConfig.SplashRadius.AsFloat;
				size = _weaponConfig.SplashRadius.AsFloat * 2f;
			}

			ShootIndicator.SetTransformState(input->AimingDirection.ToUnityVector2());
			ShootIndicator.SetVisualState(input->IsShootButtonDown, isEmptied);
			ShootIndicator.SetVisualProperties(size, 0, range);
		}

		private void HandleOnLocalPlayerAmmoEmpty(EventOnLocalPlayerAmmoEmpty callback)
		{
			ShootIndicator.SetVisualState(ShootIndicator.VisualState, true);
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
			
			SetupWeaponInfo(callback.Game.Frames.Predicted.Time, callback.WeaponSlot, playerView);
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
		
		private void InstantiatePlayerIndicators()
		{
			var loader = _services.AssetResolverService;

			for (var i = 0; i < (int) IndicatorVfxId.TOTAL; i++)
			{
				if (!loader.TryGetAssetReference<IndicatorVfxId, GameObject>((IndicatorVfxId)i, out var indicator))
				{
					_indicators[i] = null;
					continue;
				}

				var obj = Instantiate(indicator.OperationHandle.Convert<GameObject>().Result);
				
				_indicators[i] = obj.GetComponent<IIndicator>();
			}
		}
		

		private void SetupWeaponInfo(FP currentTime, WeaponSlot weaponSlot, EntityView playerView)
		{
			var configProvider = _services.ConfigsProvider;
			var specialConfigs = configProvider.GetConfigsDictionary<QuantumSpecialConfig>();
			
			_weaponConfig = configProvider.GetConfig<QuantumWeaponConfig>((int) weaponSlot.Weapon.GameId);
			_shootIndicatorId = _weaponConfig.MaxAttackAngle > 0 ? IndicatorVfxId.Cone : IndicatorVfxId.Line;
			
			ShootIndicator.SetVisualState(ShootIndicator.VisualState);

			for (var i = 0; i < Constants.MAX_SPECIALS; i++)
			{
				if (_specialIndicators[i] != null)
				{
					Destroy(((MonoBehaviour) _specialIndicators[i]).gameObject);
				}

				if (specialConfigs.TryGetValue((int) _weaponConfig.Specials[i], out var config))
				{
					_specialIndicators[i] = Instantiate((MonoBehaviour) _indicators[(int) config.Indicator]).GetComponent<IIndicator>();
					
					_specialIndicators[i].Init(playerView);
					_specialIndicators[i].SetVisualProperties(config.Radius.AsFloat * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE,
					                                          config.MinRange.AsFloat, config.MaxRange.AsFloat);
				}
				else
				{
					_specialIndicators[i] = null;
				}
			}

			SetupSpecialsInput(currentTime, weaponSlot);
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