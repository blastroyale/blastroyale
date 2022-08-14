using FirstLight.Game.Input;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service provides the necessary API to help the match control and access all local player behaviour
	/// </summary>
	public interface ILocalPlayerService
	{
		/// <summary>
		/// Requests the <see cref="EntityRef"/> of the <see cref="PlayerCharacter"/> in quantum
		/// </summary>
		EntityRef LocalPlayerEntity { get; }
		
		/// <summary>
		/// Requests the <see cref="PlayerRef"/> representing the local player in quantum
		/// </summary>
		PlayerRef LocalPlayerRef { get; }
	}
	
	/// <inheritdoc cref="ILocalPlayerService"/>
	public unsafe class LocalPlayerService : ILocalPlayerService, MatchServices.IMatchService
	{
		private readonly IGameServices _gameServices;
		private readonly IMatchServices _matchServices;
		private readonly LocalInput _localInput;
		private readonly IIndicator[] _indicators = new IIndicator[(int) IndicatorVfxId.TOTAL];
		private readonly IIndicator[] _specialIndicators = new IIndicator[Constants.MAX_SPECIALS];

		private QuantumWeaponConfig _weaponConfig;
		private IndicatorVfxId _shootIndicatorId;

		/// <inheritdoc />
		public EntityRef LocalPlayerEntity { get; private set; }
		/// <inheritdoc />
		public PlayerRef LocalPlayerRef { get; private set; }

		private IIndicator ShootIndicator => _indicators[(int)_shootIndicatorId];

		public LocalPlayerService(IGameServices gameServices, IMatchServices matchServices)
		{
			_gameServices = gameServices;
			_matchServices = matchServices;
			_localInput = new LocalInput();
		}

		public void Dispose()
		{
			LocalPlayerRef = PlayerRef.None;
			LocalPlayerEntity = EntityRef.None;
			
			_localInput?.Dispose();
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void Init(QuantumGame game)
		{
			var entity = game.GetLocalPlayerData(false, out var f).Entity;

			if (LocalPlayerEntity.IsValid  || !entity.IsValid || !f.TryGet<PlayerCharacter>(entity, out var playerCharacter))
			{
				return;
			}
			
			var playerView = _matchServices.EntityViewUpdaterService.GetManualView(entity);

			LocalPlayerRef = playerCharacter.Player;
			LocalPlayerEntity = entity;

			foreach (var indicator in _indicators)
			{
				indicator?.Init(playerView);
			}
			
			_localInput.Enable();
			SetWeaponIndicators(playerCharacter.CurrentWeapon.GameId);
			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnUpdateView);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponChanged>(this, HandleOnLocalPlayerWeaponChanged);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAmmoEmpty>(this, HandleOnLocalPlayerAmmoEmpty);
		}

		private void OnUpdateView(CallbackUpdateView callback)
		{
			var f = callback.Game.Frames.Predicted;
			
			if (!f.Unsafe.TryGetPointer<CharacterController3D>(LocalPlayerEntity, out var kcc) ||
			    !f.Unsafe.TryGetPointer<PlayerCharacter>(LocalPlayerEntity, out var playerCharacter))
			{
				return;
			}

			var playerInput = f.GetPlayerInput(LocalPlayerRef);

			OnUpdateMove(playerInput);
			OnUpdateAim(f, playerInput, playerCharacter, kcc);
			OnUpdateSpecials();
		}

		private void OnUpdateAim(Frame f, Quantum.Input* input, PlayerCharacter* playerCharacter, CharacterController3D* kcc)
		{
			var isEmptied = playerCharacter->IsAmmoEmpty(f, LocalPlayerEntity);
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

		private void OnUpdateMove(Quantum.Input* input)
		{
			_indicators[(int) IndicatorVfxId.Movement].SetTransformState(input->Direction.ToUnityVector2());
			_indicators[(int) IndicatorVfxId.Movement].SetVisualState(input->IsMoveButtonDown);
		}

		private void OnUpdateSpecials()
		{
			if (_localInput.Gameplay.SpecialButton0.WasPressedThisFrame())
			{
				_specialIndicators[0].SetVisualState(true);
			}
			else if (_localInput.Gameplay.SpecialButton0.WasReleasedThisFrame())
			{
				_specialIndicators[0].SetVisualState(false);
			}
			else if (_localInput.Gameplay.SpecialButton1.WasPressedThisFrame())
			{
				_specialIndicators[1].SetVisualState(true);
			}
			else if (_localInput.Gameplay.SpecialButton1.WasReleasedThisFrame())
			{
				_specialIndicators[1].SetVisualState(false);
			}

			if (_localInput.Gameplay.SpecialAim.inProgress)
			{
				var value = _localInput.Gameplay.SpecialAim.ReadValue<Pair<int, Vector2>>();
				
				_specialIndicators[value.Key].SetTransformState(value.Value);
			}
		}
		
		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			InstantiatePlayerIndicators();
			
			if (isReconnect)
			{
				Init(game);
			}
			else
			{
				QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, HandleOnLocalPlayerSpawned);
			}
		}

		public void OnMatchEnded()
		{
			Dispose();
		}

		private void HandleOnLocalPlayerAmmoEmpty(EventOnLocalPlayerAmmoEmpty callback)
		{
			ShootIndicator.SetVisualState(ShootIndicator.VisualState, true);
		}

		private void HandleOnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetWeaponIndicators(callback.Weapon.GameId);
		}

		private void HandleOnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			Init(callback.Game);
		}

		private void SetWeaponIndicators(GameId weapon)
		{
			var configProvider = _gameServices.ConfigsProvider;
			var specialConfigs = configProvider.GetConfigsDictionary<QuantumSpecialConfig>();
			
			_weaponConfig = configProvider.GetConfig<QuantumWeaponConfig>((int) weapon);
			_shootIndicatorId = _weaponConfig.MaxAttackAngle > 0 ? IndicatorVfxId.Cone : IndicatorVfxId.Line;
			
			ShootIndicator.SetVisualState(ShootIndicator.VisualState);

			for (var i = 0; i < Constants.MAX_SPECIALS; i++)
			{
				if (_specialIndicators[i] != null)
				{
					Object.Destroy(((MonoBehaviour) _specialIndicators[i]).gameObject);
				}

				if (specialConfigs.TryGetValue((int) _weaponConfig.Specials[i], out var config))
				{
					_specialIndicators[i] = Object.Instantiate((MonoBehaviour) _indicators[(int) config.Indicator])
					                              .GetComponent<IIndicator>();
					
					_specialIndicators[i].SetVisualProperties(config.Radius.AsFloat * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE,
					                                               config.MinRange.AsFloat, config.MaxRange.AsFloat);
				}
				else
				{
					_specialIndicators[i] = null;
				}
			}
		}
		
		private void InstantiatePlayerIndicators()
		{
			var loader = _gameServices.AssetResolverService;

			for (var i = 0; i < (int) IndicatorVfxId.TOTAL; i++)
			{
				if (!loader.TryGetAssetReference<IndicatorVfxId, GameObject>((IndicatorVfxId)i, out var indicator))
				{
					_indicators[i] = null;
					continue;
				}

				var obj = Object.Instantiate(indicator.OperationHandle.Convert<GameObject>().Result);
				
				_indicators[i] = obj.GetComponent<IIndicator>();
			}
		}
	}
}