using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Fetches the MagShotCount for your currently equipped weapon
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public class GetMagShotCountFunction : AIFunction<int>
	{
		/// <inheritdoc />
		public unsafe override int Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var pc = f.Unsafe.GetPointer<PlayerCharacter>(e);
			return pc->GetMagShotCount(f, pc->CurrentWeaponSlot, out _);
		}
	}
}