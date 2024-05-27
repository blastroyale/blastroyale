using Cinemachine;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service to abstract several camera features
	/// </summary>
	public interface IMatchCameraService
	{
		/// <summary>
		/// Shakes the camera
		/// </summary>
		/// <param name="shape">Defines which Cinemachine ImpulseShape to use</param>
		/// <param name="duration">For how long should the screen shake</param>
		/// <param name="strength">How strong is the shake</param>
		/// <param name="position">The position of the place where the shake was started from</param>
		void StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes shape, float duration, float strength,
							  Vector3 position = default);


		void SetCameras(CinemachineVirtualCamera adventureCamera);
	}

	/// <inheritdoc />
	public class MatchCameraService : IMatchCameraService, MatchServices.IMatchService
	{
		
		private IGameDataProvider _gameDataProvider;
		private IMatchServices _matchServices;
		private IGameServices _services;

		private CinemachineImpulseSource _impulseSource;
		private GameObject _cameraServiceObject;
		private CinemachineVirtualCamera _adventureCamera;

		public MatchCameraService(IGameDataProvider gameDataProvider, IMatchServices matchServices, IGameServices services)
		{
			_gameDataProvider = gameDataProvider;
			_matchServices = matchServices;
			_services = services;
			_cameraServiceObject = new GameObject("CameraService", typeof(CinemachineImpulseSource));
			_impulseSource = _cameraServiceObject.GetComponent<CinemachineImpulseSource>();

			if (FeatureFlags.DAMAGED_CAMERA_SHAKE)
			{
				QuantumEvent.SubscribeManual<EventOnEntityDamaged>(this, OnEntityDamaged);
			}

			QuantumEvent.SubscribeManual<EventOnPlayerAttack>(this, OnPlayerAttack);
			QuantumEvent.SubscribeManual<EventOnRaycastShotExplosion>(this, OnEventOnRaycastShotExplosion);
			QuantumEvent.SubscribeManual<EventOnHazardLand>(this, OnEventHazardLand);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, OnLocalSkydiveEnd);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
		}

		public void StartShotShake(Vector2 shotRotation = default, float strength = 0.04f)
		{
			if (!_services.LocalPrefsService.IsScreenShakeEnabled.Value || _adventureCamera == null)
				return;

			var newImpulse = new CinemachineImpulseDefinition
			{
				m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
				m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
				m_ImpulseDuration = 0.08f,
				m_DissipationDistance = 99999,
				m_ImpactRadius = 999999,
			};

			_impulseSource.m_ImpulseDefinition = newImpulse;
			var a = new Vector3(-shotRotation.x, 0, -shotRotation.y);
			CinemachineImpulseManager.Instance.Clear();
			newImpulse.CreateAndReturnEvent(this._adventureCamera.transform.position, a.normalized * strength);
		}


		public void StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes shape, float duration, float strength, Vector3 position = default)
		{
			if (!_services.LocalPrefsService.IsScreenShakeEnabled.Value || _adventureCamera == null)
				return;

			var newImpulse = new CinemachineImpulseDefinition
			{
				m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Dissipating,
				m_ImpulseShape = shape,
				m_ImpulseDuration = duration,
				m_DissipationDistance = GameConstants.Screenshake.SCREENSHAKE_DISSAPATION_DISTANCE_MAX,
				m_ImpactRadius = GameConstants.Screenshake.SCREENSHAKE_DISSAPATION_DISTANCE_MIN,
			};

			var vel = Random.insideUnitCircle.normalized;
			_impulseSource.m_ImpulseDefinition = newImpulse;

			var cameraSkew = _adventureCamera.transform.position - _adventureCamera.Follow.position;
			position += cameraSkew;

			_impulseSource.GenerateImpulseAtPositionWithVelocity(position, new Vector3(vel.x, 0, vel.y) * strength);
		}


		private void OnPlayerAttack(EventOnPlayerAttack ev)
		{
			var damagedPlayerIsLocal = _matchServices.SpectateService.SpectatedPlayer.Value.Player == ev.Player;
			if (!damagedPlayerIsLocal)
			{
				return;
			}

			if (!ev.PlayerEntity.IsAlive(ev.Game.Frames.Predicted)) return;
			var strenght = ev.WeaponConfig.AttackCooldown <= FP._0_99 ? 0.04f : 0.08f;
			StartShotShake(ev.Direction.ToUnityVector2(), strenght);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
		}

		public void SetCameras(CinemachineVirtualCamera adventureCamera)
		{
			_adventureCamera = adventureCamera;
		}

		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
			Object.Destroy(_cameraServiceObject);
		}

		private void OnLocalPlayerSkydiveDrop(EventOnLocalPlayerSkydiveDrop callback)
		{
			var f = callback.Game.Frames.Verified;
			if (callback.Entity.IsAlive(f))
			{
				StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes.Rumble,
					GameConstants.Screenshake.SCREENSHAKE_LARGE_DURATION, GameConstants.Screenshake.SCREENSHAKE_SMALL_STRENGTH,
					callback.Entity.GetPosition(f).ToUnityVector3());
			}
		}

		private void OnLocalSkydiveEnd(EventOnLocalPlayerSkydiveLand callback)
		{
			var f = callback.Game.Frames.Verified;
			if (callback.Entity.IsAlive(f))
			{
				StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes.Rumble,
					GameConstants.Screenshake.SCREENSHAKE_LARGE_DURATION, GameConstants.Screenshake.SCREENSHAKE_SMALL_STRENGTH,
					callback.Entity.GetPosition(f).ToUnityVector3());
			}
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes.Explosion,
				GameConstants.Screenshake.SCREENSHAKE_LARGE_DURATION, GameConstants.Screenshake.SCREENSHAKE_LARGE_STRENGTH);
		}

		private void OnEventOnRaycastShotExplosion(EventOnRaycastShotExplosion callback)
		{
			ExplosionScreenShake(callback.sourceId, callback.EndPosition.ToUnityVector3());
		}

		private void OnEventHazardLand(EventOnHazardLand callback)
		{
			ExplosionScreenShake(callback.sourceId, callback.HitPosition.ToUnityVector3());
		}

		private void OnEntityDamaged(EventOnEntityDamaged callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView) ||
				callback.Player == PlayerRef.None) // TODO: a sound for things that are not players.
			{
				return;
			}

			var damagedPlayerIsLocal = _matchServices.SpectateService.SpectatedPlayer.Value.Player == callback.Player;
			var f = callback.Game.Frames.Verified;

			if (callback.ShieldDamage > 0 && callback.HealthDamage > 0)
			{
				if (damagedPlayerIsLocal)
				{
					_matchServices.MatchCameraService.StartScreenShake(Cinemachine.CinemachineImpulseDefinition.ImpulseShapes.Recoil,
						GameConstants.Screenshake.SCREENSHAKE_SMALL_DURATION, GameConstants.Screenshake.SCREENSHAKE_MEDIUM_STRENGTH,
						callback.Entity.GetPosition(f).ToUnityVector3());
				}
			}
		}

		private void ExplosionScreenShake(GameId sourceId, Vector3 endPosition)
		{
			var shake = false;
			var shakePower = GameConstants.Screenshake.SCREENSHAKE_SMALL_STRENGTH;
			var shakeDuration = GameConstants.Screenshake.SCREENSHAKE_SMALL_DURATION;

			switch (sourceId)
			{
				case GameId.SpecialAimingGrenade:
					shake = true;
					shakePower = GameConstants.Screenshake.SCREENSHAKE_MEDIUM_STRENGTH;
					shakeDuration = GameConstants.Screenshake.SCREENSHAKE_SMALL_DURATION;
					break;
				case GameId.SpecialAimingAirstrike:
					shakePower = GameConstants.Screenshake.SCREENSHAKE_LARGE_STRENGTH;
					shakeDuration = GameConstants.Screenshake.SCREENSHAKE_MEDIUM_DURATION;
					shake = true;
					break;
				case GameId.SpecialAimingStunGrenade:
					shake = true;
					break;
				case GameId.SpecialSkyLaserBeam:
					shake = true;
					break;
				case GameId.ApoRPG:
				case GameId.ModLauncher:
				case GameId.SciCannon:
					break;
				case GameId.Barrel:
					shake = true;
					break;
			}

			if (shake)
			{
				StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes.Explosion,
					shakeDuration, shakePower, endPosition);
			}
		}
	}
}