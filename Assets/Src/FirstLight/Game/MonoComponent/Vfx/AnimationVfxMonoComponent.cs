using FirstLight.Game.Ids;
using FirstLight.Services;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// This vfx auto despawns after the defined <see cref="Animation"/> is completed
	/// </remarks>
	public class AnimationVfxMonoComponent : Vfx<VfxId>
	{
		[SerializeField, Required] private Animation _animation;

		private void OnValidate()
		{
			_animation = _animation ? _animation : GetComponent<Animation>();
		}

		protected override void OnSpawned()
		{
			Despawner(_animation.clip.length);
		}
	}
}