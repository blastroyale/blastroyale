using FirstLight.Game.Ids;
using FirstLight.Services;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// This vfx auto despawns after a certain time
	/// </remarks>
	public class MutableTimeVfxMonoComponent : Vfx<VfxId>
	{
		/// <summary>
		/// Starts the despawn timer to despawn this <see cref="Vfx{T}"/> in the given <paramref name="time"/>
		/// </summary>
		public void StartDespawnTimer(float time)
		{
			Despawner(time);
		}
	}
}