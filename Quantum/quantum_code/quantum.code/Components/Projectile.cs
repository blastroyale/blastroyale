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
			if (DamagePct != 0) dmg *= (DamagePct / FP._100);
			return dmg;
		}

		public QuantumWeaponConfig WeaponConfig(Frame f) => f.WeaponConfigs.GetConfig(SourceId); 

		public bool ShouldPerformSubProjectileOnHit(Frame f)
		{
			return WeaponConfig(f).BulletHitPrototype != null && Iteration == 0;
		}
		
		public bool ShouldPerformSubProjectileOnEndOfLifetime(Frame f)
		{
			return WeaponConfig(f).BulletEndOfLifetimePrototype != null && Iteration == 0;
		}
		
		public static void CreateProjectile(Frame f, EntityRef shooter, FP range, FPVector2 aimingDirection, FPVector3 projectileStartPosition, QuantumWeaponConfig weaponConfig)
		{
			FP accuracyMod = FP._0;
			if(weaponConfig.MinAttackAngle > FP._0 && !weaponConfig.IsMeleeWeapon)
			{
				accuracyMod = f.WeaponConfigs.GetRandomBakedAccuracyAngle(f, weaponConfig.Id);
			}
			var shotDirection = FPVector2.Rotate(aimingDirection, accuracyMod * FP.Deg2Rad).XOY;
			var directionPerTick = shotDirection * weaponConfig.AttackHitSpeed.AsInt * f.DeltaTime;
			var despawnTime = FP._0;
			if (weaponConfig.IsMeleeWeapon)
			{
				despawnTime = f.Time + FP._0_20;
				projectileStartPosition += shotDirection * range / 2;
			}
		
			var projectile = new Projectile
			{
				Attacker = shooter,
				Direction = directionPerTick,
				SourceId = weaponConfig.Id,
				RangeSquared = (range * range).AsShort,
				SpawnPosition = projectileStartPosition,
				DespawnTime = despawnTime,
				Speed = (byte)weaponConfig.AttackHitSpeed.AsInt,
				StunDuration = 0,
				Target = EntityRef.None,
				Iteration = 0,
				TeamSource = (byte)f.Get<Targetable>(shooter).Team
			};
			
			var projectileEntity = f.Create(f.FindAsset<EntityPrototype>(weaponConfig.BulletPrototype != null
				? weaponConfig.BulletPrototype.Id
				: f.AssetConfigs.DefaultBulletPrototype.Id));
			var transform = f.Unsafe.GetPointer<Transform3D>(projectileEntity);

			transform->Position = projectile.SpawnPosition;
			transform->Rotation = FPQuaternion.LookRotation(projectile.Direction, FPVector3.Up);
			
			f.Add(projectileEntity, projectile);
			
			// Only on verified
			f.Events.OnProjectileFired(projectileEntity, projectile);
			
			// Can be read from predicted 
			f.Events.OnProjectileFiredPredicted(projectileEntity, projectile);
		}
		
	}
}