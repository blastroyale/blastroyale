using FirstLight.Game.Ids;
using FirstLight.Services;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// This vfx auto despawns after a certain time
	/// </remarks>
	public class TimedVfxMonoComponent : Vfx<VfxId>
	{
		[SerializeField] private float _activeTime;
		
		protected override void OnSpawned()
		{
			Despawner(_activeTime);
		}
	}
}