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
			slot->MagazineShotCount = slot->MagazineSize;
			f.Events.OnPlayerMagazineReloaded(f.Unsafe.GetPointer<PlayerCharacter>(e)->Player, e, slot->Weapon);
		}
	}
}