using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
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
		private readonly IGameDataProvider _data;
		private EntityRef _localPlayerEntity;
		private EntityView _playerView;
		private QuantumWeaponConfig _weaponConfig;
		private IndicatorVfxId _shootIndicatorId;
		private readonly IIndicator[] _indicators = new IIndicator[(int) IndicatorVfxId.TOTAL];
		private readonly IIndicator[] _specialIndicators = new IIndicator[Constants.MAX_SPECIALS];
		private readonly IIndicator[] _specialRadiusIndicators = new IIndicator[Constants.MAX_SPECIALS];
		private WeaponAim _weaponAim;
		
		private IIndicator ShootIndicator => _indicators[(int)_shootIndicatorId];
		private IIndicator MovementIndicator => _indicators[(int) IndicatorVfxId.Movement];
		
		public LocalPlayerIndicatorContainerView()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_data = MainInstaller.Resolve<IGameDataProvider>();
			
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAmmoEmpty>(this, HandleOnLocalPlayerAmmoEmpty);
			QuantumEvent.SubscribeManual<EventOnGameEnded>(this, OnGameEnded);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponChanged>(this, OnWeaponChanged);
		}
		
		/// <inheritdoc />
		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
		}
		
		private void OnWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{ 
			SetupWeaponInfo(callback.Game.Frames.Predicted, callback.WeaponSlot.Weapon.GameId);
			SetupWeaponSpecials(callback.WeaponSlot);
		}

		public void SetupWeaponSpecials(WeaponSlot slot)
		{
			for (var i = 0; i < slot.Specials.Length; i++)
			{
				SetupIndicator(i, slot.Specials[i].SpecialId, _playerView);
			}
		}

		private void OnLocalPlayerSkydiveLand(EventOnLocalPlayerSkydiveLand callback)
		{
			GetIndicator((int)IndicatorVfxId.Movement).SetVisualProperties(1, -1, -1);
		}
		
		private void OnLocalPlayerSkydiveDrop(EventOnLocalPlayerSkydiveDrop callback)
		{
			GetIndicator((int)IndicatorVfxId.Movement).SetVisualProperties(0, -1, -1);
		}

		/// <summary>
		/// Requests the <see cref="IIndicator"/> representing the given <paramref name="idx"/>
		/// </summary>
		/// <returns></returns>
		public IIndicator GetIndicator(int idx)
		{
			return _indicators[idx];
		}
		
		/// <summary>
		/// Requests the <see cref="IIndicator"/> representing the given <paramref name="specialIdx"/>
		/// </summary>
		public IIndicator GetSpecialIndicator(int specialIdx)
		{
			return _specialIndicators[specialIdx];
		}
		
		/// <summary>
		/// Gets the special radius which is responsible to demonstrate the max range of the given special
		/// </summary>
		public IIndicator GetSpecialRadiusIndicator(int specialIdx)
		{
			return _specialRadiusIndicators[specialIdx];
		}
		
		/// <summary>
		/// Initializes this container with the player's <paramref name="playerView"/> to follow
		/// </summary>
		public void Init(EntityView playerView)
		{
			_localPlayerEntity = playerView.EntityRef;
			_playerView = playerView;
			var aim = _services.VfxService.Spawn(VfxId.WeaponAim);
			_weaponAim = aim.GetComponent<WeaponAim>();
			_weaponAim.SetView(playerView);

			for (int i = 0; i < _specialIndicators.Length; i++)
			{
				_specialIndicators[i] = null;
				_specialRadiusIndicators[i] = null;
			}

			foreach (var indicator in _indicators)
			{
				indicator?.Init(playerView);
			}
		}

		/// <summary>
		/// Updates the a move update indicators with the given data
		/// </summary>
		public void OnMoveUpdate(Vector2 direction, bool isPressed)
		{
			var moveIndicatorPosition = direction;
			if (!_data.AppDataProvider.MovespeedControl)
			{
				moveIndicatorPosition = direction.normalized;
				moveIndicatorPosition /= 2;
			}
			_indicators[(int) IndicatorVfxId.Movement]?.SetTransformState(moveIndicatorPosition);
			_indicators[(int) IndicatorVfxId.Movement]?.SetVisualState(isPressed);
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
			if (_data.AppDataProvider.ConeAim || _weaponConfig.IsMeleeWeapon)
			{
				ShootIndicator.SetVisualState(false);
				if (_weaponConfig.MaxAttackAngle == 0)
				{
					_shootIndicatorId = IndicatorVfxId.Line;
				}
				else
				{
					_shootIndicatorId = IndicatorVfxId.Cone;
				}
				if (f.Context.TryGetMutatorByType(MutatorType.AbsoluteAccuracy, out _))
				{
					_shootIndicatorId = _weaponConfig.NumberOfShots > 1 ? IndicatorVfxId.Cone : IndicatorVfxId.Line;
				}
				ShootIndicator.SetVisualState(ShootIndicator.VisualState);
			}
			else
			{
				_weaponAim.UpdateWeapon(f, _localPlayerEntity, _weaponConfig);
			}
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
			
			if (FeatureFlags.SPECIAL_RADIUS)
			{
				if (_specialRadiusIndicators[index] == null)
				{
					_specialRadiusIndicators[index] = Object.Instantiate((MonoBehaviour) _indicators[(int)IndicatorVfxId.Range])
						.GetComponent<IIndicator>();
				}
			
				_specialRadiusIndicators[index].Init(playerView);
				_specialRadiusIndicators[index].SetVisualProperties(config.MaxRange.AsFloat, 
					config.MaxRange.AsFloat, config.MaxRange.AsFloat);
				_specialIndicators[index].Init(playerView);
			}
			_specialIndicators[index].SetVisualProperties(config.Radius.AsFloat * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE_NON_PLAIN_INDICATORS,
			                                              config.MinRange.AsFloat, config.MaxRange.AsFloat);
		}

		private void LegacyConeAim(Frame f, FPVector2 aim, bool shooting)
		{
			if (!f.Unsafe.TryGetPointer<CharacterController3D>(_localPlayerEntity, out var kcc) ||
				!f.Unsafe.TryGetPointer<PlayerCharacter>(_localPlayerEntity, out var playerCharacter))
			{
				return;
			}

			if (FeatureFlags.QUANTUM_PREDICTED_AIM)
			{
				aim = f.GetPlayerInput(playerCharacter->Player)->AimingDirection;
			}

			var isEmptied = playerCharacter->IsAmmoEmpty(f, _localPlayerEntity);
			var reloading = playerCharacter->WeaponSlot->MagazineShotCount == 0;
			var transform = f.Unsafe.GetPointer<Transform3D>(_localPlayerEntity);
			var aimDirection = QuantumHelpers.GetAimDirection(aim, ref transform->Rotation).Normalized.ToUnityVector2();
			var rangeStat = f.Get<Stats>(_localPlayerEntity).GetStatData(StatType.AttackRange).StatValue;
			var range = QuantumHelpers.GetDynamicAimValue(kcc, rangeStat, rangeStat + _weaponConfig.AttackRangeAimBonus).AsFloat;
			var minAttackAngle = _weaponConfig.MaxAttackAngle == 0 ? 0 : _weaponConfig.MinAttackAngle;
			var maxAttackAngle = _weaponConfig.MaxAttackAngle == 0 ? 0 :_weaponConfig.MaxAttackAngle;
			var lerp = QuantumHelpers.GetDynamicAimValue(kcc,maxAttackAngle, minAttackAngle).AsFloat;
			var angleInRad = maxAttackAngle == minAttackAngle || f.Context.TryGetMutatorByType(MutatorType.AbsoluteAccuracy, out _) 
				? minAttackAngle : lerp;

			// We use a formula to calculate the scale of a shooting indicator
			float size = Mathf.Max(0.5f, Mathf.Tan(angleInRad * 0.5f * Mathf.Deg2Rad) * range * 2f);

			// For a melee weapon with a splash damage we use a separate calculation for an indicator
			if (_weaponConfig.IsMeleeWeapon && _weaponConfig.SplashRadius > FP._0)
			{
				range += _weaponConfig.SplashRadius.AsFloat;
				size = _weaponConfig.SplashRadius.AsFloat * 2f;
			}

			var isAiming = shooting || aim != FPVector2.Zero;
			
			ShootIndicator.SetTransformState(aimDirection);
			ShootIndicator.SetVisualState(isAiming, isEmptied || reloading);
			ShootIndicator.SetVisualProperties(size, 0, range);
		}

		public void OnUpdateAim(Frame f, FPVector2 aim, bool shooting)
		{
			if (!_localPlayerEntity.IsAlive(f)) return;
			
			MovementIndicator?.SetVisualState(!shooting && aim == FPVector2.Zero);
			if (_data.AppDataProvider.ConeAim || _weaponConfig.IsMeleeWeapon)
			{
				LegacyConeAim(f, aim, shooting);
			}
			else
			{
				_weaponAim.UpdateAimAngle(f, _localPlayerEntity, aim);
			}
		}

		private void HandleOnLocalPlayerAmmoEmpty(EventOnLocalPlayerAmmoEmpty callback)
		{
			ShootIndicator.SetVisualState(ShootIndicator.VisualState, true);
		}
		
		private void OnGameEnded(EventOnGameEnded callback)
		{
			ShootIndicator?.SetVisualState(false);
		}
	}
}