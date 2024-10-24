using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <summary>
	/// Handles the danger indicators vfx from the enemy attacks
	/// </summary>
	public class DangerIndicatorVfxMonoBehaviour : VfxMonoBehaviour
	{
		/// <summary>
		/// Initializes the danger indicator in the given <paramref name="spawnPosition"/> to live over the given <paramref name="lifetime"/>
		/// </summary>
		public void Init(Vector3 spawnPosition, float lifetime, float indicationRadius)
		{
			var scale = indicationRadius * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE_NON_PLAIN_INDICATORS * Vector3.one;
			var cacheTransform = transform;
			
			cacheTransform.localScale = scale;
			cacheTransform.position = spawnPosition;
			
			Despawner(lifetime).Forget();
		}
	}
}