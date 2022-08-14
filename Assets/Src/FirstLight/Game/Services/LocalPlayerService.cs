using System.Threading.Tasks;
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
		private readonly IIndicator[] _indicators = new IIndicator[(int) IndicatorVfxId.TOTAL];
		private readonly Pair<ITransformIndicator, QuantumSpecialConfig>[] _specialIndicators =
			new Pair<ITransformIndicator, QuantumSpecialConfig>[Constants.MAX_SPECIALS];

		private QuantumWeaponConfig _weaponConfig;
		private Pair<ITransformIndicator, QuantumSpecialConfig> _specialAimIndicator;
		private ITransformIndicator _shootIndicator;
		private ITransformIndicator _movementIndicator;

		/// <inheritdoc />
		public EntityRef LocalPlayerEntity { get; private set; }
		/// <inheritdoc />
		public PlayerRef LocalPlayerRef { get; private set; }

		public LocalPlayerService(IGameServices gameServices, IMatchServices matchServices)
		{
			_gameServices = gameServices;
			_matchServices = matchServices;
		}

		public void Dispose()
		{
			LocalPlayerRef = PlayerRef.None;
			LocalPlayerEntity = EntityRef.None;
			
			// TODO: Dispose the indicators
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
			
			SetWeaponIndicators(playerCharacter.CurrentWeapon.GameId);
			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnUpdateView);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponChanged>(this, HandleOnLocalPlayerWeaponChanged);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAmmoEmpty>(this, HandleOnLocalPlayerAmmoEmpty);
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

			_shootIndicator.SetTransformState(input->AimingDirection.ToUnityVector2());
			_shootIndicator.SetVisualState(input->IsShootButtonDown, isEmptied);
			_shootIndicator.SetVisualProperties(size, 0, range);
		}

		private void OnUpdateMove(Quantum.Input* input)
		{
			_movementIndicator.SetTransformState(input->Direction.ToUnityVector2());
			_movementIndicator.SetVisualState(input->IsMoveButtonDown);
		}
		
/*
		/// <inheritdoc />
		public void OnSpecialAim(InputAction.CallbackContext context)
		{
			_specialAimIndicator.Key?.SetTransformState(context.ReadValue<Vector2>());
		}

		/// <inheritdoc />
		public void OnSpecialButton0(InputAction.CallbackContext context)
		{
			var isDown = context.ReadValueAsButton();
			var config = _specialIndicators[0].Value;

			_specialAimIndicator.Key?.SetVisualState(false);

			_specialAimIndicator =
				isDown ? _specialIndicators[0] : new Pair<ITransformIndicator, QuantumSpecialConfig>();

			_specialAimIndicator.Key?.SetVisualState(true);
			_specialAimIndicator.Key?.SetTransformState(Vector2.zero);
			_specialAimIndicator.Key
			                    ?
			                    .SetVisualProperties(config.Radius.AsFloat * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE,
			                                         config.MinRange.AsFloat, config.MaxRange.AsFloat);
		}

		/// <inheritdoc />
		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			var isDown = context.ReadValueAsButton();
			var config = _specialIndicators[1].Value;

			_specialAimIndicator.Key?.SetVisualState(false);

			_specialAimIndicator =
				isDown ? _specialIndicators[1] : new Pair<ITransformIndicator, QuantumSpecialConfig>();


			_specialAimIndicator.Key?.SetVisualState(true);
			_specialAimIndicator.Key?.SetTransformState(Vector2.zero);
			_specialAimIndicator.Key?.SetVisualProperties(config.Radius.AsFloat * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE,
			                                              config.MinRange.AsFloat, config.MaxRange.AsFloat);
		}*/

		private void HandleOnLocalPlayerAmmoEmpty(EventOnLocalPlayerAmmoEmpty callback)
		{
			var shootState = _shootIndicator?.VisualState ?? false;

			_shootIndicator?.SetVisualState(shootState, true);
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
			var shootState = _shootIndicator?.VisualState ?? false;
			
			_weaponConfig = configProvider.GetConfig<QuantumWeaponConfig>((int) weapon);
			
			var indicator = _weaponConfig.MaxAttackAngle > 0 ? IndicatorVfxId.Cone : IndicatorVfxId.Line;

			_shootIndicator = _indicators[(int) indicator] as ITransformIndicator;
			_shootIndicator?.SetVisualState(shootState);

			for (var i = 0; i < Constants.MAX_SPECIALS; i++)
			{
				var pair = new Pair<ITransformIndicator, QuantumSpecialConfig>();

				if (specialConfigs.TryGetValue((int) _weaponConfig.Specials[i], out var specialConfig))
				{
					pair.Key = _indicators[(int) specialConfig.Indicator] as ITransformIndicator;
					pair.Value = specialConfig;
				}

				_specialIndicators[i] = pair;
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
				
				_indicators[i] = indicator.OperationHandle.Convert<GameObject>().Result.GetComponent<IIndicator>();
			}
		}
	}
}