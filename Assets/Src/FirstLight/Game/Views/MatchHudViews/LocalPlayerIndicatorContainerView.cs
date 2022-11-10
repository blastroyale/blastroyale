using System;
using FirstLight.Game.Configs;
using System.Threading.Tasks;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This view simply contains all the local player indicators controller view
	/// </summary>
	public unsafe class LocalPlayerIndicatorContainerView : IDisposable
	{
		private readonly IGameServices _services;
		private EntityRef _localPlayerEntity;
		private QuantumWeaponConfig _weaponConfig;
		private IndicatorVfxId _shootIndicatorId;
		private readonly IIndicator[] _indicators = new IIndicator[(int) IndicatorVfxId.TOTAL];
		private readonly IIndicator[] _specialIndicators = new IIndicator[Constants.MAX_SPECIALS];

		private IIndicator ShootIndicator => _indicators[(int)_shootIndicatorId];
		
		public LocalPlayerIndicatorContainerView(IGameServices services)
		{
			_services = services;
			InstantiateAllIndicators();
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAmmoEmpty>(this, HandleOnLocalPlayerAmmoEmpty);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		/// <summary>
		/// Requests the <see cref="IIndicator"/> representing the given <paramref name="specialIdx"/>
		/// </summary>
		public IIndicator GetIndicator(int specialIdx)
		{
			return _specialIndicators[specialIdx];
		}
		
		/// <summary>
		/// Initializes this container with the player's <paramref name="playerView"/> to follow
		/// </summary>
		public void Init(EntityView playerView)
		{
			_localPlayerEntity = playerView.EntityRef;
			
			for (int i = 0; i < _specialIndicators.Length; i++)
			{
				_specialIndicators[i] = null;
			}

			foreach (var indicator in _indicators)
			{
				indicator?.Init(playerView);
			}
		}

		/// <summary>
		/// Updates this container of <see cref="IIndicator"/>
		/// </summary>
		public void OnUpdate(Frame f)
		{
			if (!f.Unsafe.TryGetPointer<CharacterController3D>(_localPlayerEntity, out var kcc) ||
			    !f.Unsafe.TryGetPointer<PlayerCharacter>(_localPlayerEntity, out var playerCharacter))
			{
				return;
			}

			var playerInput = f.GetPlayerInput(playerCharacter->Player);

			OnUpdateAim(f, playerInput, playerCharacter, kcc);
		}

		/// <summary>
		/// Updates the a move update indicators with the given data
		/// </summary>
		public void OnMoveUpdate(Vector2 direction, bool isPressed)
		{
			_indicators[(int) IndicatorVfxId.Movement].SetTransformState(direction);
			_indicators[(int) IndicatorVfxId.Movement].SetVisualState(isPressed);
		}
		
		/// <summary>
		///  Instantiates all possible indicators
		/// </summary>
		public void InstantiateAllIndicators()
		{
			var loader = _services.AssetResolverService;

			for (var i = 0; i < (int) IndicatorVfxId.TOTAL; i++)
			{
				if (!loader.TryGetAssetReference<IndicatorVfxId, GameObject>((IndicatorVfxId)i, out var indicator))
				{
					_indicators[i] = null;
					continue;
				}
				
				var obj =  _services.AssetResolverService.RequestAsset<IndicatorVfxId, GameObject>((IndicatorVfxId)i, false, true).Result;
				_indicators[i] = obj.GetComponent<IIndicator>();
			}
		}

		/// <summary>
		/// Setups all the indicators with the given data
		/// </summary>
		public void SetupWeaponInfo(Frame f, GameId weaponId)
		{
			_weaponConfig = _services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) weaponId);
			_shootIndicatorId = _weaponConfig.MaxAttackAngle > 0  ? IndicatorVfxId.Cone : IndicatorVfxId.Line;
			if (f.Context.TryGetMutatorByType(MutatorType.AbsoluteAccuracy, out _))
			{
				_shootIndicatorId = _weaponConfig.NumberOfShots > 1 ? IndicatorVfxId.Cone : IndicatorVfxId.Line;
			}

			ShootIndicator.SetVisualState(ShootIndicator.VisualState);
		}

		/// <summary>
		/// Setups the indicator configs for the specials
		/// </summary>
		public void SetupIndicator(int index, GameId specialId, EntityView playerView)
		{
			_services.ConfigsProvider.TryGetConfig<QuantumSpecialConfig>((int)specialId, out var config);
			
			if (_specialIndicators[index] != null)
			{
				Object.Destroy( ((MonoBehaviour) _specialIndicators[index]).gameObject);
			}
			
			_specialIndicators[index] = Object.Instantiate((MonoBehaviour) _indicators[(int) config.Indicator])
			                                  .GetComponent<IIndicator>();
					
			_specialIndicators[index].Init(playerView);
			_specialIndicators[index].SetVisualProperties(config.Radius.AsFloat * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE,
			                                              config.MinRange.AsFloat, config.MaxRange.AsFloat);
		}

		private void OnUpdateAim(Frame f, Quantum.Input* input, PlayerCharacter* playerCharacter, CharacterController3D* kcc)
		{
			var isEmptied = playerCharacter->IsAmmoEmpty(f, _localPlayerEntity);
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(_localPlayerEntity);
			var transform = f.Unsafe.GetPointer<Transform3D>(_localPlayerEntity);

			var aimDirection = new Vector2(QuantumHelpers.GetAimDirection(f, bb, transform).X.AsFloat, QuantumHelpers.GetAimDirection(f, bb, transform).Y.AsFloat);
			var rangeStat = f.Get<Stats>(_localPlayerEntity).GetStatData(StatType.AttackRange).StatValue;
			var range = QuantumHelpers.GetDynamicAimValue(kcc, rangeStat, rangeStat + _weaponConfig.AttackRangeAimBonus).AsFloat;

			var minAttackAngle = _shootIndicatorId == IndicatorVfxId.Line ? 0 : _weaponConfig.MinAttackAngle;
			var maxAttackAngle = _shootIndicatorId == IndicatorVfxId.Line ? 0 :_weaponConfig.MaxAttackAngle;

			var lerp = QuantumHelpers.GetDynamicAimValue(kcc,maxAttackAngle, minAttackAngle).AsFloat;
			var angleInRad = maxAttackAngle == minAttackAngle || f.Context.TryGetMutatorByType(MutatorType.AbsoluteAccuracy, out _) 
				? minAttackAngle : lerp;
			
			// We use a formula to calculate the scale of a shooting indicator
			var size = Mathf.Max(0.5f, Mathf.Tan(angleInRad * 0.5f * Mathf.Deg2Rad) * range * 2f);

			// For a melee weapon with a splash damage we use a separate calculation for an indicator
			if (_weaponConfig.IsMeleeWeapon && _weaponConfig.SplashRadius > FP._0)
			{
				range += _weaponConfig.SplashRadius.AsFloat;
				size = _weaponConfig.SplashRadius.AsFloat * 2f;
			}

			ShootIndicator.SetTransformState(aimDirection.normalized);
			ShootIndicator.SetVisualState(input->IsShootButtonDown, isEmptied);
			ShootIndicator.SetVisualProperties(size, 0, range);
		}

		private void HandleOnLocalPlayerAmmoEmpty(EventOnLocalPlayerAmmoEmpty callback)
		{
			ShootIndicator.SetVisualState(ShootIndicator.VisualState, true);
		}
	}
}