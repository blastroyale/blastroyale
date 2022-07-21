using FirstLight.Game.Ids;
using FirstLight.Services;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// This vfx auto despawns after the defined <see cref="ParticleSystem"/> is completed
	/// </remarks>
	public class ParticleVfxMonoComponent : Vfx<VfxId>
	{
		[SerializeField, Required] private ParticleSystem _particle;

		private void OnValidate()
		{
			_particle = _particle ? _particle : GetComponent<ParticleSystem>();
		}
		
		protected override void OnSpawned()
		{
			Despawner(_particle.main.startLifetime.constant);
		}
	}
}