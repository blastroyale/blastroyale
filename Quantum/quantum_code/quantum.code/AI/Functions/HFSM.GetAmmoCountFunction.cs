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
		public override FP Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			return f.Get<Stats>(e).CurrentAmmo;
		}
	}
}
