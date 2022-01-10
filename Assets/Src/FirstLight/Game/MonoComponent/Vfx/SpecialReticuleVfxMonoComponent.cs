using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <summary>
	/// Handles the special reticule vfx when a airstrike/grenade and other similar
	/// projectile-based specials are triggered by the player
	/// </summary>
	public class SpecialReticuleVfxMonoComponent : VfxMonoComponent
	{
		private EntityRef _specialProjectile;

		/// <summary>
		/// Sets the special reticule <paramref name="targetPosition"/> of the given special's projectile <paramref name="projectile"/>
		/// </summary>
		public void SetTarget(Vector3 targetPosition, EntityRef projectile, float radius)
		{
			var scale = radius * GameConstants.RadiusToScaleConversionValue * Vector3.one;
			scale.y = 1f;

			var transformCache = transform;
			transformCache.position = targetPosition;
			transformCache.localScale = scale;
			_specialProjectile = projectile;
			
			QuantumEvent.Subscribe<EventOnProjectileHit>(this, OnEventOnProjectileHit);
		}

		protected override void OnDespawned()
		{
			_specialProjectile = EntityRef.None;

			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnEventOnProjectileHit(EventOnProjectileHit callback)
		{
			if (callback.HitData.Projectile == _specialProjectile)
			{
				Despawn();
			}
		}
	}
}