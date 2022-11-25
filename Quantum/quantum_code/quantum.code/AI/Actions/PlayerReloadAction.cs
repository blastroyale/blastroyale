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
			var pc = f.Unsafe.GetPointer<PlayerCharacter>(e);
			pc->WeaponSlots.GetPointer(pc->CurrentWeaponSlot)->MagazineShotCount = pc->WeaponSlots.GetPointer(pc->CurrentWeaponSlot)->MagazineSize;
		}
	}
}