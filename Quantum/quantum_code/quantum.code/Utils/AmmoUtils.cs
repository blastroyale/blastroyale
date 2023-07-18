using Photon.Deterministic;

namespace Quantum
{
	public class AmmoUtils
	{
		public static int GetMaxAmmo(Frame f, GameId weaponId)
		{
			var id = f.WeaponConfigs.GetConfig(weaponId);
			return id.MaxAmmo;
		}

		/// <summary>
		/// It includes the ammo in the magazine
		/// </summary>
		/// <returns></returns>
		public static unsafe int GetCurrentAmmoForGivenWeapon(Frame f, EntityRef player, in WeaponSlot slot)
		{
			if (!slot.Weapon.IsValid())
			{
				return 0;
			}


			// Ammo should not be in STATS!!!!!!!!!!! coringando
			if (!f.Unsafe.TryGetPointer<Stats>(player, out var stats)) return 0;

			var weaponId = slot.Weapon.GameId;
			var ammo = FPMath.CeilToInt(stats->CurrentAmmoPercent * GetMaxAmmo(f, weaponId));
			// If this value is -1 the weapon does not use magazine 
			if (slot.MagazineShotCount > 0)
			{
				ammo += slot.MagazineShotCount;
			}

			return ammo;
		}

		/// <summary>
		/// Returns the current ammo percentage for the player
		/// If the player doesn't have a weapon it returns 0
		/// </summary>
		public static unsafe FP GetCurrentAmmoPercentage(Frame f, EntityRef player)
		{
			var pc = f.Unsafe.GetPointer<PlayerCharacter>(player);
			var weapon = pc->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY];
			if (!weapon.Weapon.IsValid())
			{
				return FP._0;
			}
			return !f.Unsafe.TryGetPointer<Stats>(player, out var stats) ? FP._0 : stats->CurrentAmmoPercent;
		}
	}
}