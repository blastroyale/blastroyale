using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <summary>
	/// Handles the special reticule vfx when a airstrike/grenade and other similar
	/// projectile-based specials are triggered by the player
	/// </summary>
	public class SpecialReticuleVfxMonoBehaviour : VfxMonoBehaviour
	{
		/// <summary>
		/// Sets the special reticule <paramref name="targetPosition"/> of the given special's projectile <paramref name="projectile"/>
		/// </summary>
		public void SetTarget(Vector3 targetPosition, float radius, float endTime)
		{
			var transformCache = transform;
			var scale = radius * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE_NON_PLAIN_INDICATORS * Vector3.one;
			
			scale.y = 1f;
			transformCache.position = targetPosition;
			transformCache.localScale = scale;
			
			Despawner(endTime).Forget();
		}
	}
}