using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Shows any visual feedback for the projectiles created in the scene via quantum.
	/// This object is only responsible for the view.
	/// </summary>
	public class ProjectileViewMonoComponent : EntityViewBase
	{
		[SerializeField] private VfxId _projectileHitVfx;
		[SerializeField] private VfxId _projectileFailedHitVfx;
		[SerializeField] private VfxId _projectileSplashVfx = VfxId.SplashProjectile;

		protected override void OnInit()
		{
			QuantumEvent.Subscribe<EventOnProjectileHit>(this, OnEventOnProjectileHit);
			QuantumEvent.Subscribe<EventOnProjectileFailedHitDestroy>(this, OnProjectileFailedHitDestroy);
		}

		private void OnProjectileFailedHitDestroy(EventOnProjectileFailedHitDestroy callback)
		{
			if (callback.Projectile != EntityRef)
			{
				return;
			}
			
			Services.VfxService.Spawn(_projectileFailedHitVfx).transform.position = transform.position;
		}

		private void OnEventOnProjectileHit(EventOnProjectileHit callback)
		{
			if (callback.HitData.Projectile != EntityRef)
			{
				return;
			}
			
			var hitPosition = callback.HitData.IsStaticHit ? transform.position : callback.HitData.HitPosition.ToUnityVector3();
			
			// If it's not Splash then we create either FailedHitVfx (if it hits the wall) or regular HitVfx in other cases
			if (callback.ProjectileData.SplashRadius == FP._0)
			{
				var vfx = callback.HitData.IsStaticHit ? _projectileFailedHitVfx : _projectileHitVfx;
				
				Services.VfxService.Spawn(vfx).transform.position = hitPosition;
				
				return;
			}
			
			// If it's Splash then we create regular HitVfx and then SplashProjectile Vfx
			var regularHitVfx = Services.VfxService.Spawn(_projectileHitVfx);
			var splashProjectile = Services.VfxService.Spawn(_projectileSplashVfx);
			var scale = (callback.ProjectileData.SplashRadius * 2).AsFloat * Vector3.one;
			var projectileTransform = splashProjectile.transform;
			
			regularHitVfx.transform.position = hitPosition;
			projectileTransform.position = hitPosition;
			projectileTransform.localScale = scale;
		}
	}
}