using FirstLight.Game.Ids;
using FirstLight.Services;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// Use this MonoComponent if you want to attach a basic <see cref="Vfx{T}"/> to a prefab
	/// </remarks>
	public class VfxMonoComponent : Vfx<VfxId>
	{
	}
}