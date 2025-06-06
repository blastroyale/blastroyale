using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
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
	public class LocalPlayerIndicatorContainerView : IDisposable
	{
		private readonly IGameServices _services;
		
		private EntityRef _localPlayerEntity;
		private EntityView _playerView;
		private QuantumWeaponConfig _weaponConfig;
		private AudioWeaponConfig _weaponAudioConfig;
		private IndicatorVfxId _shootIndicatorId;
		private readonly IIndicator[] _indicators = new IIndicator[(int) IndicatorVfxId.TOTAL];
		private readonly IIndicator[] _specialIndicators = new IIndicator[Constants.MAX_SPECIALS];

		private readonly RangeIndicatorMonoComponent[] _specialRadiusIndicators =
			new RangeIndicatorMonoComponent[Constants.MAX_SPECIALS];

		private WeaponAim _weaponAim;

		private IIndicator ShootIndicator => _indicators[(int) _shootIndicatorId];

		private float _shrinkingCircleRadius = -1;
		private Vector2 _shrinkCircleCenter;
		private int _shrinkingCircleStartTime;

		private bool _localPlayerDropped;
		private bool _localPlayerLanded;

		public LocalPlayerIndicatorContainerView()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			QuantumEvent.SubscribeManual<EventOnPlayerAmmoChanged>(this, HandleOnLocalPlayerAmmoEmpty);
			QuantumEvent.SubscribeManual<EventOnGameEnded>(this, OnGameEnded);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerWeaponChanged>(this, OnWeaponChanged);
			QuantumEvent.SubscribeManual<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle);
		}

		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			_shrinkingCircleRadius = callback.ShrinkingCircle.TargetRadius.AsFloat;
			_shrinkCircleCenter = callback.ShrinkingCircle.TargetCircleCenter.ToUnityVector2();
			_shrinkingCircleStartTime = callback.ShrinkingCircle.ShrinkingStartTime;

			((SafeAreaIndicatorMonoComponent) _indicators[(int) IndicatorVfxId.SafeArea])?.SetSafeArea(
				_shrinkCircleCenter, _shrinkingCircleRadius, _shrinkingCircleStartTime);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
			foreach (var i in _indicators) DestroyIndicator(i);
			foreach (var i in _specialIndicators) DestroyIndicator(i);
			foreach (var i in _specialRadiusIndicators) DestroyIndicator(i);
			if (!_weaponAim.IsDestroyed()) GameObject.Destroy(_weaponAim);
		}

		public bool IsInitialized() => _playerView != null;

		private void OnWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			if (!IsInitialized()) return;
			SetupWeaponInfo(callback.Game.Frames.Predicted, callback.WeaponSlot.Weapon.GameId);
		}

		public void SetupSpecials(PlayerInventory inventory)
		{
			for (var i = 0; i < inventory.Specials.Length; i++)
			{
				SetupIndicator(i, inventory.Specials[i].SpecialId);
			}
		}

		private void OnLocalPlayerSkydiveLand(EventOnLocalPlayerSkydiveLand callback)
		{
			_localPlayerLanded = true;
			if (!IsInitialized()) return;
		
			GetIndicator((int) IndicatorVfxId.Movement)?.SetVisualProperties(1, -1, -1);
		}

		private void OnLocalPlayerSkydiveDrop(EventOnLocalPlayerSkydiveDrop callback)
		{
			_localPlayerDropped = true;
			if (!IsInitialized()) return;
			GetIndicator((int) IndicatorVfxId.Movement)?.SetVisualProperties(0, -1, -1);
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
		public RangeIndicatorMonoComponent GetSpecialRadiusIndicator(int specialIdx)
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
			var aim = MainInstaller.ResolveMatchServices().VfxService.Spawn(VfxId.WeaponAim);
			_weaponAim = aim.GetComponent<WeaponAim>();
			_weaponAim.SetView(playerView);
			_weaponAim.gameObject.SetActive(false);

			for (int i = 0; i < _specialIndicators.Length; i++)
			{
				_specialIndicators[i] = null;
				_specialRadiusIndicators[i] = null;
			}
			
			foreach (var indicator in _indicators)
			{
				try
				{
					indicator?.Init(playerView);
				}
				catch (Exception e)
				{
					FLog.Error("Error initializing indicator", e);
				}
			}

			((SafeAreaIndicatorMonoComponent) _indicators[(int) IndicatorVfxId.SafeArea])?.SetSafeArea(
				_shrinkCircleCenter, _shrinkingCircleRadius, _shrinkingCircleStartTime);

			if (_localPlayerDropped)
			{
				GetIndicator((int) IndicatorVfxId.Movement)?.SetVisualProperties(0, -1, -1);
			}

			if (_localPlayerLanded)
			{
				GetIndicator((int) IndicatorVfxId.Movement)?.SetVisualProperties(1, -1, -1);
			}
		}

		/// <summary>
		/// Updates the a move update indicators with the given data
		/// </summary>
		public void OnMoveUpdate(Vector2 direction, bool isPressed)
		{
			var moveIndicatorPosition = direction.normalized / 2f;

			_indicators[(int) IndicatorVfxId.Movement]?.SetTransformState(moveIndicatorPosition);
			_indicators[(int) IndicatorVfxId.Movement]?.SetVisualState(isPressed);
		}

		/// <summary>
		///  Instantiates all possible indicators
		/// </summary>
		public async UniTask InstantiateAllIndicators()
		{
			var loader = _services.AssetResolverService;

			for (var i = 0; i < (int) IndicatorVfxId.TOTAL; i++)
			{
				if (!loader.TryGetAssetReference<IndicatorVfxId, GameObject>((IndicatorVfxId) i, out var indicator))
				{
					_indicators[i] = null;
					continue;
				}

				var obj = await _services.AssetResolverService
					.RequestAsset<IndicatorVfxId, GameObject>((IndicatorVfxId) i, false, true);

				_indicators[i] = obj.GetComponent<IIndicator>();
			}

			OnIndicatorsLoaded();
		}

		private void OnIndicatorsLoaded()
		{
			if (_localPlayerLanded)
			{
				GetIndicator((int) IndicatorVfxId.Movement)?.SetVisualProperties(1, -1, -1);
			}
			else if (_localPlayerDropped)
			{
				GetIndicator((int) IndicatorVfxId.Movement)?.SetVisualProperties(0, -1, -1);
			}
		}

		/// <summary>
		/// Setups all the indicators with the given data
		/// </summary>
		public void SetupWeaponInfo(Frame f, GameId weaponId)
		{
			_weaponConfig = _services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) weaponId);
			_weaponAudioConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int) weaponId);
			ShootIndicator?.SetVisualState(false);
			_weaponAim.gameObject.SetActive(false);
			if (_weaponConfig.IsMeleeWeapon)
			{
				_shootIndicatorId = _weaponConfig.MinAttackAngle == 0 ? IndicatorVfxId.Line : IndicatorVfxId.Cone;
				ShootIndicator?.SetVisualState(ShootIndicator.VisualState);
			}
			else
			{
				_weaponAim.UpdateWeapon(f, _localPlayerEntity, _weaponConfig);
			}
		}

		private void DestroyIndicator(IIndicator i)
		{
			if (i is MonoBehaviour component && !component.IsDestroyed())
			{
				Object.Destroy(component);
			}
		}

		/// <summary>
		/// Setups the indicator configs for the specials
		/// </summary>
		public void SetupIndicator(int index, GameId specialId)
		{
			if (_services.RoomService.IsLocalPlayerSpectator) return;
			_services.ConfigsProvider.TryGetConfig<QuantumSpecialConfig>((int) specialId, out var config);
			if (config == null) return;

			if (_specialIndicators[index] != null)
			{
				Object.Destroy(((MonoBehaviour) _specialIndicators[index]).gameObject);
			}

			_specialIndicators[index] = Object.Instantiate((MonoBehaviour) _indicators[(int) config.Indicator])
				.GetComponent<IIndicator>();

			if (_specialRadiusIndicators[index] == null)
			{
				_specialRadiusIndicators[index] = Object
					.Instantiate((MonoBehaviour) _indicators[(int) IndicatorVfxId.Range])
					.GetComponent<RangeIndicatorMonoComponent>();
			}

			_specialRadiusIndicators[index].Init(_playerView);
			_specialRadiusIndicators[index].SetVisualProperties(config.MaxRange.AsFloat,
				config.MaxRange.AsFloat, config.MaxRange.AsFloat);
			_specialIndicators[index].Init(_playerView);

			_specialIndicators[index].SetVisualProperties(
				config.Radius.AsFloat * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE_NON_PLAIN_INDICATORS,
				config.MinRange.AsFloat, config.MaxRange.AsFloat);
		}

		private unsafe void LegacyConeAim(Frame f, FPVector2 aim, bool shooting)
		{
			if (!f.Unsafe.TryGetPointer<TopDownController>(_localPlayerEntity, out var kcc) ||
				!f.Unsafe.TryGetPointer<PlayerCharacter>(_localPlayerEntity, out var playerCharacter))
			{
				return;
			}

			aim = f.GetPlayerInput(playerCharacter->Player)->AimingDirection;

			var isEmptied = playerCharacter->IsAmmoEmpty(f, _localPlayerEntity);
			var reloading = playerCharacter->SelectedWeaponSlot->MagazineShotCount == 0;
			var aimDirection = aim;
			var rangeStat = f.Get<Stats>(_localPlayerEntity).GetStatData(StatType.AttackRange).StatValue.AsFloat;

			// We use a formula to calculate the scale of a shooting indicator
			float size = Mathf.Max(0.5f,
				Mathf.Tan(_weaponConfig.MinAttackAngle * 0.5f * Mathf.Deg2Rad) * rangeStat * 2f);

			// For a melee weapon with a splash damage we use a separate calculation for an indicator
			if (_weaponConfig.IsMeleeWeapon && _weaponConfig.SplashRadius > FP._0)
			{
				rangeStat += _weaponConfig.SplashRadius.AsFloat;
				size = _weaponConfig.SplashRadius.AsFloat * 2f;
			}

			ShootIndicator.SetTransformState(aim.ToUnityVector2());
			ShootIndicator.SetVisualState(shooting, isEmptied || reloading);
			ShootIndicator.SetVisualProperties(size, 0, rangeStat);
		}

		public void OnUpdateAim(Frame f, FPVector2 aim, bool shooting)
		{
			if (!_localPlayerEntity.IsAlive(f)) return;

			if (_weaponConfig.IsMeleeWeapon)
			{
				//LegacyConeAim(f, aim, shooting);
			}
			else
			{
				// Handling wind up and down sounds here; e.g. Minigun
				if (_weaponAudioConfig.WeaponShotWindUpId != AudioId.None) //(_weaponConfig.Id == GameId.ApoMinigun)
				{
					if (shooting & !_weaponAim.gameObject.activeSelf)
					{
						_services.AudioFxService.PlayClip2D(_weaponAudioConfig.WeaponShotWindUpId, GameConstants.Audio.MIXER_GROUP_SFX_3D_ID);
					}
					else if (!shooting & _weaponAim.gameObject.activeSelf)
					{
						_services.AudioFxService.PlayClip2D(_weaponAudioConfig.WeaponShotWindDownId, GameConstants.Audio.MIXER_GROUP_SFX_3D_ID);
					}
				}

				_weaponAim.gameObject.SetActive(shooting);
				if (shooting)
				{
					_weaponAim.UpdateAimAngle(f, _localPlayerEntity, aim);
				}
			}
		}

		private void HandleOnLocalPlayerAmmoEmpty(EventOnPlayerAmmoChanged callback)
		{
			if (!IsInitialized() || _localPlayerEntity != callback.Entity)
				return;

			if (callback.CurrentAmmo == 0)
			{
				_weaponAim.SetColor(Color.red);
			}
			else
			{
				_weaponAim.ResetColor();
			}

			ShootIndicator.SetVisualState(ShootIndicator.VisualState, true);
		}

		private void OnGameEnded(EventOnGameEnded callback)
		{
			ShootIndicator?.SetVisualState(false);
		}
	}
}