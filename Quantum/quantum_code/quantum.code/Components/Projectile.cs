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
		public FP GetPower(in Frame f)
		{
			var weaponConfig = f.WeaponConfigs.GetConfig(SourceId);
			if (!f.TryGet<Stats>(Attacker, out var stats)) return 0;
			var dmg = stats.GetStatData(StatType.Power).StatValue * weaponConfig.PowerToDamageRatio;
			if (f.Unsafe.TryGetPointer<PlayerCharacter>(Attacker, out var player))
			{
				if (player->CurrentWeapon.Material == EquipmentMaterial.Golden)
				{
					dmg *= f.WeaponConfigs.GoldenGunDamageModifier;
				}
			}
			if (DamagePct != 0) dmg *= (DamagePct / FP._100);
			return dmg;
		}

		public QuantumWeaponConfig WeaponConfig(Frame f) => f.WeaponConfigs.GetConfig(SourceId); 

		public bool ShouldPerformSubProjectileOnHit(Frame f)
		{
			return WeaponConfig(f).BulletHitPrototype != null && Iteration == 0;
		}

		public bool IsSubProjectile()
		{
			return Iteration > 0;
		}

		public bool ConfigHasSubprojectile(Frame f)
		{
			return WeaponConfig(f).BulletEndOfLifetimePrototype != null;
		}
		
		public bool ShouldPerformSubProjectileOnEndOfLifetime(Frame f)
		{
			return ConfigHasSubprojectile(f) && !IsSubProjectile();
		}
	}
}