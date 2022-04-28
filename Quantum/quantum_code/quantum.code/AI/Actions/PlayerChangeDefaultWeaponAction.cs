using System;

namespace Quantum
{
	/// <summary>
	/// This action changes the <see cref="PlayerCharacter"/> current weapon to the Melee weapon
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerChangeDefaultWeapon : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			f.Unsafe.GetPointer<PlayerCharacter>(e)->EquipSlotWeapon(f, e, 0);
		}
	}
}