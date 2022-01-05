using System;

namespace Quantum
{
	/// <summary>
	/// This action processes when the <see cref="PlayerCharacter"/> <see cref="Weapon"/> is empty
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe class PlayerWeaponEmptyAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var weapon = f.Get<Weapon>(e);
			var specials = weapon.Specials;
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			
			if (weapon.WeaponId == Constants.DEFAULT_WEAPON_GAME_ID)
			{
				return;
			}
			
			f.Events.OnLocalPlayerWeaponEmpty(f.Get<PlayerCharacter>(e).Player, e);
			
			for (var i = 0; i < specials.Length; i++)
			{
				if (specials[i].IsSpecialAvailable(f))
				{
					return;
				}
			}
			
			playerCharacter->SetWeapon(f, e, Constants.DEFAULT_WEAPON_GAME_ID, ItemRarity.Common, 1);
		}
	}
}