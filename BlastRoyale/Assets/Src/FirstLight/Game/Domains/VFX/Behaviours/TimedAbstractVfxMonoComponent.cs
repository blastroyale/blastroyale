using FirstLight.Game.Domains.VFX;
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
	public class TimedVfxMonoBehaviour : VfxMonoBehaviour
	{
		[SerializeField] private float _activeTime;
		
		protected override void OnSpawned()
		{
			Despawner(_activeTime).Forget();
		}
	}
}