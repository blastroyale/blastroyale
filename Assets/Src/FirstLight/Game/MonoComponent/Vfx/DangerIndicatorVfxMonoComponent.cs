using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <summary>
	/// Handles the danger indicators vfx from the enemy attacks
	/// </summary>
	public class DangerIndicatorVfxMonoComponent : VfxMonoComponent
	{
		/// <summary>
		/// Initializes the danger indicator in the given <paramref name="spawnPosition"/> to live over the given <paramref name="lifetime"/>
		/// </summary>
		public void Init(Vector3 spawnPosition, float lifetime, float indicationRadius)
		{
			var scale = indicationRadius * GameConstants.RadiusToScaleConversionValue * Vector3.one;
			var cacheTransform = transform;
			
			cacheTransform.localScale = scale;
			cacheTransform.position = spawnPosition;
			
			Despawner(lifetime);
		}
	}
}