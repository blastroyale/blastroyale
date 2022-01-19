using System;

namespace Quantum
{
	/// <summary>
	/// This action changes the <see cref="PlayerCharacter"/> <see cref="Weapon"/> to the default weapon
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerChangeDefaultWeapon : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			
			playerCharacter->SetWeapon(f, e, Constants.DEFAULT_WEAPON_GAME_ID, ItemRarity.Common, 1);
		}
	}
}