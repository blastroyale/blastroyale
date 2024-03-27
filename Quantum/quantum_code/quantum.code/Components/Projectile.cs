using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Projectile
	{
		/// <summary>
		/// Calculates the power of a projectile.
		/// This is only calculated when the projectile hits as opposed to every fired projectile to both
		/// save memory for not having this on every projectile entity and not having to calculate unecessarily
		/// for when a project misses.
		/// Edge case: Player shoots with weapon A and swaps to weapon B before the projectile hits.
		/// </summary>
		public readonly FP GetPower(in Frame f)
		{
			var weaponConfig = f.WeaponConfigs.GetConfig(SourceId);
			if (!f.TryGet<Stats>(Attacker, out var stats)) return 0;
			var dmg = stats.GetStatData(StatType.Power).StatValue * weaponConfig.PowerToDamageRatio;
			if (f.Unsafe.TryGetPointer<PlayerCharacter>(Attacker, out var player))
			{
				if (player->CurrentWeapon.Material == EquipmentMaterial.Golden)
				{
					var goldenDmg = dmg * f.WeaponConfigs.GoldenGunDamageModifier;
					
					// We ensure that golden weapon deals at least +1 more damage per bullet
					dmg = (goldenDmg - dmg) < FP._1 ? dmg + FP._1 : goldenDmg;
				}
			}

			if (DamagePct != 0) dmg *= (DamagePct / FP._100);
			return dmg;
		}


		public readonly bool IsSubProjectile()
		{
			return Iteration > 0;
		}

		private readonly bool ConfigHasSubprojectile(Frame f)
		{
			if (f.WeaponConfigs.TryGetConfig(SourceId, out var config))
			{
				return config.BulletEndOfLifetimePrototype != null;
			}

			return false;
		}

		public readonly bool ConfigIsMelee(Frame f)
		{
			return f.WeaponConfigs.TryGetConfig(SourceId, out var config) && config.IsMeleeWeapon;
		}

		public readonly bool IsSubProjectileAOE(Frame f)
		{
			return f.WeaponConfigs.TryGetConfig(SourceId, out var config) && config.HitType == SubProjectileHitType.AreaOfEffect;
		}

		public bool ShouldPerformSubProjectileOnEndOfLifetime(Frame f)
		{
			return ConfigHasSubprojectile(f) && !IsSubProjectile();
		}
	}
}