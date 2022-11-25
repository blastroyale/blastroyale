using Photon.Deterministic;
using System;

namespace Quantum
{
	/// <summary>
	/// Returns the player's current ammo count
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
					   GenerateAssetResetMethod = false)]
	public class GetAmmoCountFunction : AIFunction<FP>
	{
		/// <inheritdoc />
		public unsafe override FP Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			return f.Unsafe.GetPointer<Stats>(e)->CurrentAmmo;
		}
	}
}
