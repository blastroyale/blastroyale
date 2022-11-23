using System;

namespace Quantum
{
	/// <summary>
	/// This action processes when the <see cref="PlayerCharacter"/> <see cref="Weapon"/> is empty
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
			var weaponConfig = f.WeaponConfigs.GetConfig(pc->CurrentWeapon.GameId);

			pc->WeaponSlots[pc->CurrentWeaponSlot].MagazineShotCount = weaponConfig.MagazineSize;
		}
	}
}