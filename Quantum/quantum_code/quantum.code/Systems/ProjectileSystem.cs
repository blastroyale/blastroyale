using System.Diagnostics;
using Photon.Deterministic;
using Quantum.Systems.Bots;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Projectile"/> collisions
	/// </summary>
	public unsafe class ProjectileSystem : SystemMainThreadFilter<ProjectileSystem.ProjectileFilter>, ISignalOnTriggerEnter3D
	{
		public struct ProjectileFilter
		{
			public EntityRef Entity;
			public Projectile* Projectile;
			public Transform3D* Transform;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref ProjectileFilter filter)
		{
			if ((filter.Transform->Position - filter.Projectile->SpawnPosition).SqrMagnitude > filter.Projectile->RangeSquared)
			{
				if (filter.Projectile->ShouldPerformSubProjectileOnEndOfLifetime(f))
				{
					CreateSubProjectile(f, *filter.Projectile, filter.Transform->Position, false);
				}
				// Projectile that performs Sub Projectile at end of lifetime is not considered as failed
				else
				{
					f.Events.OnProjectileFailedHit(filter.Entity, *filter.Projectile, filter.Transform->Position);
				}
				
				f.Destroy(filter.Entity);
				return;
			}

			if (filter.Projectile->DespawnTime != FP._0 && f.Time > filter.Projectile->DespawnTime)
			{
				f.Destroy(filter.Entity);
				return;
			}
			
			filter.Transform->Position += filter.Projectile->Direction;
		}
		
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			if (!f.TryGet<Projectile>(info.Entity, out var projectile) || info.Other == info.Entity || info.StaticData.IsTrigger || projectile.Attacker == info.Entity 
				|| f.Has<EntityDestroyer>(info.Entity) || (projectile.Attacker == info.Other && !projectile.IsSubProjectile()) || (projectile.Attacker != info.Other && TeamHelpers.HasSameTeam(f, projectile.Attacker, info.Other)))
			{
				return;
			}
			
			if (info.Other.IsValid)
			{
				
				var cfg = projectile.WeaponConfig(f);
				// For melee we need to have LOS between attacker and target
				if (cfg.IsMeleeWeapon && !QuantumHelpers.HasMapLineOfSight(f, projectile.Attacker, info.Other))
				{
					return;
				}

				// For area of effects, we need to have LOS between the projectile and the target
				if (projectile.IsSubProjectile() && cfg.HitType == SubProjectileHitType.AreaOfEffect)
				{
					if (!QuantumHelpers.HasMapLineOfSight(f, info.Entity, info.Other))
					{
						return;
					}
				}
			}

			OnProjectileHit(f, info.Other, info.Entity, projectile);
		}

		/// <summary>
		/// Can be used for sub projectiles (e.g area explosions, fire fields, ricochets etc)
		/// This creates a sub-projectile based on the parent projectile just changing its entity prototype and
		/// a couple specific veriables specified per projectile hit type
		/// </summary>
		private void CreateSubProjectile(Frame f, Projectile p, FPVector3 hitPosition, bool onHit)
		{
			var cfg = f.WeaponConfigs.GetConfig(p.SourceId);
			var subProjectile = p;
			if (cfg.HitType == SubProjectileHitType.AreaOfEffect)
			{
				subProjectile.Speed = 0;
				subProjectile.Direction = FPVector3.Zero;
				subProjectile.SpawnPosition = hitPosition;
				subProjectile.DespawnTime = f.Time + FP._0_50;
				subProjectile.DamagePct = (byte)cfg.SplashDamageRatio.AsInt;
			}
			
			subProjectile.Iteration = (byte)(p.Iteration + 1);
			var subId = onHit ? cfg.BulletHitPrototype.Id : cfg.BulletEndOfLifetimePrototype.Id;
			var entity = f.Create(f.FindAsset<EntityPrototype>(subId));
			var transform = f.Unsafe.GetPointer<Transform3D>(entity);
			transform->Position = hitPosition;
			f.Add(entity, subProjectile);
		}

		private void OnProjectileHit(Frame f, EntityRef targetHit, EntityRef projectileEntity, Projectile projectile)
		{
			
			var position = f.Get<Transform3D>(projectileEntity).Position;

			if(targetHit == projectileEntity || !targetHit.IsValid)
				f.Events.OnProjectileFailedHit(projectileEntity, projectile, position);
			else
				f.Events.OnProjectileSuccessHit(projectile, targetHit, position);
			
			if (projectile.ShouldPerformSubProjectileOnHit(f))
			{
				CreateSubProjectile(f, projectile, position, true);
			}
			else
			{

				var isSelfAOE = projectile.Attacker == targetHit && projectile.IsSubProjectile();
				var power = (uint)(projectile.GetPower(f) * (isSelfAOE ? Constants.SELF_DAMAGE_MODIFIER : FP._1));
				
				var spell = Spell.CreateInstant(f, targetHit, projectile.Attacker, projectileEntity, power,
					projectile.KnockbackAmount, position, isSelfAOE ? 0 : projectile.TeamSource);
				if (QuantumHelpers.ProcessHit(f, &spell))
				{
					OnHit(f, &spell);
				}
			}

			if (projectile.Speed > 0)
			{
				if (projectile.ShouldPerformSubProjectileOnEndOfLifetime(f))
				{
					CreateSubProjectile(f, projectile, position, false);
				}
				f.Destroy(projectileEntity);
			}
		}

		private void OnHit(Frame f, Spell* spell)
		{
			var source = f.Get<Projectile>(spell->SpellSource);
			
			if (source.StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, spell->Victim, StatusModifierType.Stun, source.StunDuration);
			}
			
			f.Events.OnProjectileTargetableHit(spell->SpellSource, spell->Victim, spell->OriginalHitPosition);
		}

		/// <summary>
		/// Main method. Called when someone shoots.
		/// </summary>
		/// <param name="f"></param>
		/// <param name="e"></param>
		public static void Shoot(Frame f, EntityRef e)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var aimDirection = bb->GetVector2(f, Constants.AimDirectionKey);
			if(f.Unsafe.TryGetPointer<BotCharacter>(e, out var bot) && bot->Target.IsValid && bb->GetBoolean(f, Constants.IsAimPressedKey))
			{
				if (bot->SharpShootNextShot)
				{
					bot->SharpShootNextShot = false;
					if (bot->TrySharpShoot(e, f, bot->Target, out var sharpDirection))
					{
						aimDirection = sharpDirection;
					}
				}
				QuantumHelpers.LookAt2d(transform, aimDirection, FP._0);
			}
			var position = transform->Position + (transform->Rotation * playerCharacter->ProjectileSpawnOffset);
			var aimingDirection = QuantumHelpers.GetAimDirection(aimDirection, ref transform->Rotation).Normalized;
			var rangeStat = f.Get<Stats>(e).GetStatData(StatType.AttackRange).StatValue;
			playerCharacter->ReduceMag(f, e); //consume a shot from your magazine
			bb->Set(f, Constants.BurstShotCount, bb->GetFP(f, Constants.BurstShotCount) - 1);
			bb->Set(f, Constants.LastShotAt, f.Time);
			f.Events.OnPlayerAttack(playerCharacter->Player, e, playerCharacter->CurrentWeapon, weaponConfig, aimingDirection, rangeStat);
			if (weaponConfig.NumberOfShots == 1 || weaponConfig.IsMeleeWeapon)
			{
				CreateProjectile(f, e, rangeStat, aimingDirection, position, weaponConfig);
			}
			else
			{
				FP max = weaponConfig.MinAttackAngle;
				FP angleStep = weaponConfig.MinAttackAngle / weaponConfig.NumberOfShots;
				FP angle = -max/ FP._2;
				for (var x = 0; x < weaponConfig.NumberOfShots; x++)
				{
					var burstDirection = FPVector2.Rotate(aimingDirection, angle * FP.Deg2Rad).XOY;
					CreateProjectile(f, e, rangeStat, burstDirection.XZ, position, weaponConfig);
					angle += angleStep;
				}
			}
		}
		
		private static void CreateProjectile(Frame f, EntityRef shooter, FP range, FPVector2 aimingDirection, FPVector3 projectileStartPosition, QuantumWeaponConfig weaponConfig)
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