using Photon.Deterministic;
using System;

namespace Quantum
{
	/// <summary>
	/// This action will refill the MagShotCOunt of the <see cref="PlayerCharacter"/>'s current weapon
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class PlayerReloadAction : AIAction
	{
		/// <inheritdoc />
		public unsafe override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			// Do the reload here
			var pc = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var stats = f.Unsafe.GetPointer<Stats>(e);
			var slot = pc->SelectedWeaponSlot;
			var diff = FPMath.Min(stats->GetCurrentAmmo(), slot->MagazineSize - slot->MagazineShotCount).AsInt;

			if(diff > 0)
			{
				slot->MagazineShotCount += diff;
				stats->ReduceAmmo(f, e, diff);
				f.Events.OnPlayerMagazineReloaded(pc->Player, e, slot->Weapon);
			}
		}
	}
}