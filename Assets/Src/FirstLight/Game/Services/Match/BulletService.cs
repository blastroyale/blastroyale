using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using Photon.Deterministic;
using Quantum;
using UnityEngine;


namespace FirstLight.Game.MonoComponent.Match
{
	public interface IBulletService
	{

	}

	/// <summary>
	/// This class handles showing/hiding player renderers inside and outside of visibility volumes based on various factors
	/// </summary>
	public class BulletService : IBulletService, MatchServices.IMatchService
	{
		private IGameServices _gameServices;
		private IMatchServices _matchServices;
		private IEntityViewUpdaterService _entityViewUpdater;
		private Dictionary<EntityRef, GameObject> _hitEffects = new();
		
		public BulletService(IGameServices gameServices, IMatchServices matchServices)
		{
			_matchServices = matchServices;
			_gameServices = gameServices;
			_hitEffects = new();
			QuantumEvent.SubscribeManual<EventOnProjectileSuccessHit>(this, OnProjectileHit);
			QuantumEvent.SubscribeManual<EventOnProjectileFailedHit>(this, OnProjectileFailedHit);
			QuantumCallback.SubscribeManual<CallbackGameDestroyed>(this, OnGameDestroyed);
		}

		private void OnGameDestroyed(CallbackGameDestroyed c) => _hitEffects.Clear();
		
		private void OnProjectileFailedHit(EventOnProjectileFailedHit ev)
		{
			if (ev.Game.Frames.Verified.IsCulled(ev.ProjectileEntity) && ev.Projectile.Attacker != _matchServices.SpectateService.GetSpectatedEntity())
			{
				return;
			}

			if (!ev.HitWall) return;
			
			_gameServices.VfxService.Spawn(VfxId.ProjectileFailedSmoke).transform.position = ev.LastPosition.ToUnityVector3();
		}

		private void PlayHitEffect(EventOnProjectileSuccessHit ev)
		{
			var fx = GetHitEffect(ev);
			if (fx == null) return;
			foreach (var particle in fx.GetComponentsInChildren<ParticleSystem>())
			{
				particle.Stop();
				particle.time = 0;
				fx.transform.position = ev.HitPosition.ToUnityVector3();
				fx.SetActive(true);
				particle.Play();
			}
		}

		private void OnProjectileHit(EventOnProjectileSuccessHit ev)
		{
			var isMine = ev.Projectile.Attacker == _matchServices.SpectateService.GetSpectatedEntity() || ev.HitEntity == _matchServices.SpectateService.GetSpectatedEntity();
			if (ev.Game.Frames.Verified.IsCulled(ev.HitEntity) && !isMine)
			{
				return;
			}

			if (!ev.Game.Frames.Predicted.TryGet<Transform3D>(ev.Projectile.Attacker, out var shooterLocation))
			{
				return;
			}

			if (FPVector3.DistanceSquared(shooterLocation.Position, ev.HitPosition) >= 2)
			{
				PlayHitEffect(ev);
			}

			if (_matchServices.SpectateService.SpectatedPlayer?.Value == null)
			{
				return;
			}
			
			var localPlayer = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;
			if (ev.HitEntity != localPlayer && ev.Projectile.Attacker != localPlayer)
			{
				return;
			}
			
			if (!_matchServices.EntityViewUpdaterService.TryGetView(ev.HitEntity, out var attackerView))
			{
				return;
			}

			var character = attackerView.GetComponent<PlayerCharacterMonoComponent>();
			character?.PlayerView?.UpdateAdditiveColor(GameConstants.Visuals.HIT_COLOR, 0.2f);
		}
		
		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}
		
		private bool IsInitialized(EventOnProjectileSuccessHit ev)
		{
			if (!_hitEffects.ContainsKey(ev.HitEntity))
			{
				_gameServices.AssetResolverService.RequestAsset<VfxId, GameObject>(VfxId.SplashProjectile, true, true,
					(id, gameObject, _) =>
					{
						_hitEffects[ev.HitEntity] = gameObject;
						OnProjectileHit(ev);
					});
				return false;
			}

			return true;
		}

		[CanBeNull]
		public GameObject GetHitEffect(EventOnProjectileSuccessHit ev)
		{
			if (!IsInitialized(ev)) return null;
			if (!_hitEffects.TryGetValue(ev.HitEntity, out var ef)) return null;
			return ef;
		}

	
		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			
		}
	}
}