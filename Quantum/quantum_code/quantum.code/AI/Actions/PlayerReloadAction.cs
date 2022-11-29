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
			//do the reload here
			var slot = f.Unsafe.GetPointer<PlayerCharacter>(e)->WeaponSlot;
			var stats = f.Unsafe.GetPointer<Stats>(e);
			var diff = FPMath.Min(stats->CurrentAmmo, slot->MagazineSize - slot->MagazineShotCount).AsInt;
			var ammoCost = (stats->GetStatData(StatType.AmmoCapacity).StatValue / f.WeaponConfigs.GetConfig(slot->Weapon.GameId).MaxAmmo.Get(f)).AsInt;
			if(diff > 0)
			{
				stats->ReduceAmmo(f, e, diff * ammoCost);
				slot->MagazineShotCount += diff;
				f.Events.OnPlayerMagazineReloaded(f.Unsafe.GetPointer<PlayerCharacter>(e)->Player, e, slot->Weapon);
			}
		}
	}
}