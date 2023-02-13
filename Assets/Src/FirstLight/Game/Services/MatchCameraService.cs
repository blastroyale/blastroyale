using Cinemachine;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
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

		private CinemachineImpulseSource _impulseSource;
		private GameObject _cameraServiceObject;
		private CinemachineVirtualCamera _adventureCamera;
		
		public MatchCameraService(IGameDataProvider gameDataProvider, IMatchServices matchServices)
		{
			_gameDataProvider = gameDataProvider;
			_matchServices = matchServices;
			_cameraServiceObject = new GameObject("CameraService", typeof(CinemachineImpulseSource));
			_impulseSource = _cameraServiceObject.GetComponent<CinemachineImpulseSource>();
			
			QuantumEvent.SubscribeManual<EventOnEntityDamaged>(this, OnEntityDamaged);
			QuantumEvent.SubscribeManual<EventOnRaycastShotExplosion>(this, OnEventOnRaycastShotExplosion);
			QuantumEvent.SubscribeManual<EventOnHazardLand>(this, OnEventHazardLand);
			QuantumEvent.SubscribeManual<EventOnProjectileExplosion>(this, OnEventOnProjectileExplosion);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, OnLocalSkydiveEnd);
		}

		public void StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes shape, float duration, float strength, Vector3 position = default)
		{
			if(!_gameDataProvider.AppDataProvider.UseScreenShake)
				return;

			var newImpulse = new CinemachineImpulseDefinition
			{
				m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Dissipating,
				m_DissipationRate = GameConstants.Screenshake.SCREENSHAKE_DISSAPATION_RATE_DEFAULT,
				m_ImpulseShape = shape,
				m_ImpulseDuration = duration,
				m_DissipationDistance = 15,
				m_ImpactRadius = GameConstants.Screenshake.SCREENSHAKE_DISSAPATION_DISTANCE_MIN,
			};

			var vel = Random.insideUnitCircle.normalized;
			_impulseSource.m_ImpulseDefinition = newImpulse;

			var cameraSkew = _adventureCamera.transform.position - _adventureCamera.Follow.position;
			position += cameraSkew;
			
			_impulseSource.GenerateImpulseAtPositionWithVelocity(position, new Vector3(vel.x, 0, vel.y) * strength);
		}

		public void SetCameras(CinemachineVirtualCamera adventureCamera)
		{
			_adventureCamera = adventureCamera;
		}

		public void Dispose()
		{
			Object.Destroy(_cameraServiceObject);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
		}

		private void OnLocalSkydiveEnd(EventOnLocalPlayerSkydiveLand callback)
		{
			var f = callback.Game.Frames.Verified;
			StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes.Rumble, 
				GameConstants.Screenshake.SCREENSHAKE_LARGE_DURATION, GameConstants.Screenshake.SCREENSHAKE_SMALL_STRENGTH,
				callback.Entity.GetPosition(f).ToUnityVector3());
		}
		
		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes.Explosion,
				GameConstants.Screenshake.SCREENSHAKE_LARGE_DURATION, GameConstants.Screenshake.SCREENSHAKE_LARGE_STRENGTH);
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			// If not a kill of spectated player, or spectated player committed suicide
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Entity != callback.EntityKiller ||
				callback.EntityKiller == callback.EntityDead)
			{
				return;
			}
			
			var shakePower = GameConstants.Screenshake.SCREENSHAKE_SMALL_STRENGTH;

			// Kill SFX
			switch (callback.CurrentMultiKill)
			{
				case 3:
					shakePower = GameConstants.Screenshake.SCREENSHAKE_MEDIUM_STRENGTH;
					break;

				case 4:
					shakePower = GameConstants.Screenshake.SCREENSHAKE_MEDIUM_STRENGTH;
					break;

				case 5:
					shakePower = GameConstants.Screenshake.SCREENSHAKE_LARGE_STRENGTH;
					break;

				default:
					if (callback.CurrentMultiKill > 5)
					{
						shakePower = GameConstants.Screenshake.SCREENSHAKE_LARGE_STRENGTH;
					}

					break;
			}
			
			StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes.Bump, 
				GameConstants.Screenshake.SCREENSHAKE_SMALL_DURATION, shakePower);
		}
		
		private void OnEventOnRaycastShotExplosion(EventOnRaycastShotExplosion callback)
		{
			ExplosionScreenShake(callback.sourceId, callback.EndPosition.ToUnityVector3());
		}

		private void OnEventHazardLand(EventOnHazardLand callback)
		{
			ExplosionScreenShake(callback.sourceId, callback.HitPosition.ToUnityVector3());
		}

		private void OnEventOnProjectileExplosion(EventOnProjectileExplosion callback)
		{
			ExplosionScreenShake(callback.sourceId, callback.EndPosition.ToUnityVector3());
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
				if(damagedPlayerIsLocal)
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
			
			if(shake)
			{
				StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes.Explosion, 
					shakeDuration, shakePower, endPosition);
			}
		}
	}
}