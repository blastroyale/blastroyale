using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using Photon.Deterministic;
using Quantum;
using Quantum.Systems;
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
			if (ev.Game.Frames.Predicted.IsCulled(ev.ProjectileEntity))
			{
				return;
			}
			_gameServices.VfxService.Spawn(VfxId.ProjectileFailedSmoke).transform.position = ev.LastPosition.ToUnityVector3();
		}

		private void OnProjectileHit(EventOnProjectileSuccessHit ev)
		{
			if (ev.Game.Frames.Predicted.IsCulled(ev.HitEntity))
			{
				return;
			}
			
			if (!ev.Game.Frames.Predicted.TryGet<Transform3D>(ev.Projectile.Attacker, out var shooterLocation))
			{
				return;
			}

			if (FPVector3.DistanceSquared(shooterLocation.Position, ev.HitPosition) >= 2)
			{
				var fx = GetHitEffect(ev.HitEntity);
				if (fx == null) return;
				var particle = fx.GetComponentInChildren<ParticleSystem>();
				particle.Stop();
				particle.time = 0;
				fx.transform.position = ev.HitPosition.ToUnityVector3();
				fx.transform.LookAt(shooterLocation.Position.ToUnityVector3());
				fx.SetActive(true);
				particle.Play();
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
			character?.PlayerView?.UpdateColor(GameConstants.Visuals.HIT_COLOR, 0.2f);
		}
		
		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}
		
		private bool IsInitialized(EntityRef entity)
		{
			if (!_hitEffects.ContainsKey(entity))
			{
				_gameServices.AssetResolverService.RequestAsset<VfxId, GameObject>(VfxId.SplashProjectile, true, true,
					(id, gameObject, _) => _hitEffects[entity] = gameObject);
				return false;
			}

			return true;
		}

		[CanBeNull]
		public GameObject GetHitEffect(EntityRef player)
		{
			if (!IsInitialized(player)) return null;
			if (!_hitEffects.TryGetValue(player, out var ef)) return null;
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