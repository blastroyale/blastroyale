using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Services;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// This vfx auto despawns after the defined <see cref="ParticleSystem"/> is completed
	/// </remarks>
	public class ParticleVfxMonoComponent : Vfx<VfxId>
	{
		[SerializeField] private ParticleSystem _particle;

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