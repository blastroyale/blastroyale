using System.Collections.Generic;
using System.Diagnostics;
using Photon.Deterministic;
using Quantum.Profiling;
using Quantum.Systems.Bots;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Projectile"/> collisions
	/// </summary>
	public unsafe class ProjectileSystem : SystemMainThreadFilter<ProjectileSystem.ProjectileFilter>, ISignalOnTriggerEnter2D
	{
		public static readonly FPVector2 CAMERA_CORRECTION = new FPVector2(FP._0_25, FP._0_50);
		
		public struct ProjectileFilter
		{
			public EntityRef Entity;
			public Projectile* Projectile;
			public Transform2D* Transform;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref ProjectileFilter filter)
		{
			if (filter.Projectile->Blocked || (filter.Transform->Position - filter.Projectile->SpawnPosition).SqrMagnitude >= filter.Projectile->RangeSquared)
			{
				if (filter.Projectile->ShouldPerformSubProjectileOnEndOfLifetime(f))
				{
					CreateSubProjectile(f, filter.Projectile, filter.Transform->Position, false);
				}
				// Projectile that performs Sub Projectile at end of lifetime is not considered as failed
				else
				{
					f.Events.OnProjectileFailedHitPredicted(filter.Entity, *filter.Projectile, filter.Transform->Position, false);
				}

				f.Events.OnProjectileEndOfLife(filter.Projectile->SourceId, filter.Transform->Position, false, filter.Projectile->IsSubProjectile());
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
		
		public void OnTriggerEnter2D(Frame f, TriggerInfo2D info)
		{
			if (!f.Unsafe.TryGetPointer<Projectile>(info.Entity, out var projectile) || info.Other == info.Entity || info.StaticData.IsTrigger || projectile->Attacker == info.Entity 
				|| f.Has<EntityDestroyer>(info.Entity) || (projectile->Attacker == info.Other && !projectile->IsSubProjectile()) || (QuantumFeatureFlags.TEAM_IGNORE_COLLISION && projectile->Attacker != info.Other && TeamSystem.HasSameTeam(f, projectile->Attacker, info.Other)))
			{
				return;
			}
			
			if (info.Other.IsValid)
			{
				// For melee we need to have LOS between attacker and target
				if (projectile->ConfigIsMelee(f) && !QuantumHelpers.HasMapLineOfSight(f, projectile->Attacker, info.Other))
				{
					return;
				}

				// For area of effects, we need to have LOS between the projectile and the target
				if (projectile->IsSubProjectile() && projectile->IsSubProjectileAOE(f))
				{
					if (!QuantumHelpers.HasMapLineOfSight(f, info.Entity, info.Other))
					{
						return;
					}
				}
				else if(projectile->Blocked)
				{
					return;
				}
			}
			OnProjectileHit(f, info.Other, info.Entity, projectile);
		}

		/// <summary>
		/// Can be used for sub projectiles (e.g area explosions, fire fields, ricochets etc)
		/// This creates a sub-projectile based on the parent projectile just changing its entity prototype and
		/// a couple specific veriables specified per projectile hit type
		/// </summary>
		private void CreateSubProjectile(Frame f, Projectile* p, in FPVector2 hitPosition, in bool onHit)
		{
			var cfg = f.WeaponConfigs.GetConfig(p->SourceId);
			var subProjectile = *p;
			if (cfg.HitType == SubProjectileHitType.AreaOfEffect)
			{
				subProjectile.Speed = 0;
				subProjectile.Direction = FPVector2.Zero;
				subProjectile.SpawnPosition = hitPosition;
				subProjectile.DespawnTime = f.Time + FP._0_50;
				subProjectile.DamagePct = (byte)cfg.SplashDamageRatio.AsInt;
			}
			
			subProjectile.Iteration = (byte)(p->Iteration + 1);
			var subId = onHit ? cfg.BulletHitPrototype.Id : cfg.BulletEndOfLifetimePrototype.Id;
			var entity = f.Create(f.FindAsset<EntityPrototype>(subId));
			var transform = f.Unsafe.GetPointer<Transform2D>(entity);
			transform->Position = hitPosition;
			f.Add(entity, subProjectile);
		}

		private void OnProjectileHit(Frame f, in EntityRef targetHit, in EntityRef projectileEntity, Projectile* projectile)
		{
			var position = f.Unsafe.GetPointer<Transform2D>(projectileEntity)->Position;
			var isTeamHit = TeamSystem.HasSameTeam(f, projectile->Attacker, targetHit);
			var spawnSubOnEof = projectile->ShouldPerformSubProjectileOnEndOfLifetime(f);

			var projectileCopy = *projectile;
			if (!QuantumFeatureFlags.TEAM_IGNORE_COLLISION && isTeamHit && !projectile->IsSubProjectile() && !spawnSubOnEof)
			{
				f.Events.OnProjectileFailedHitPredicted(projectileEntity, projectileCopy, position, false);
				f.Destroy(projectileEntity);
				return;
			}
			HostProfiler.Start("OnProjectileHit");
			
			var isSelfAOE = projectile->Attacker == targetHit && projectile->IsSubProjectile();
			var power = (uint)(projectile->GetPower(f) * (isSelfAOE ? Constants.SELF_DAMAGE_MODIFIER : FP._1));
			
			if(targetHit == projectileEntity || !targetHit.IsValid)
				f.Events.OnProjectileFailedHitPredicted(projectileEntity, projectileCopy, position, true);
			else
				f.Events.OnProjectileSuccessHitPredicted(projectileCopy, targetHit, position, power);
			
			f.Events.OnProjectileEndOfLife(projectile->SourceId, position, true,projectile->IsSubProjectile());
			
			var spell = Spell.CreateInstant(f, targetHit, projectile->Attacker, projectileEntity, power,
											projectile->KnockbackAmount, position, isSelfAOE ? 0 : projectile->TeamSource, projectile->ShotNumber);
				
			if (QuantumHelpers.ProcessHit(f, &spell))
			{
				OnHit(f, &spell);
			}
			
			if (spawnSubOnEof)
			{
				CreateSubProjectile(f, projectile, position, false);
			}

			// We dont destroy projectiles that can multi-hit
			if (!projectile->IsSubProjectile() && !projectile->ConfigIsMelee(f))
			{
				f.Destroy(projectileEntity);
			}
			HostProfiler.End();
		}

		private void OnHit(Frame f, Spell* spell)
		{
			var source = f.Unsafe.GetPointer<Projectile>(spell->SpellSource);
			
			if (source->StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, spell->Victim, StatusModifierType.Stun, source->StunDuration);
			}
			
			f.Events.OnProjectileTargetableHit(spell->SpellSource, spell->Victim, spell->OriginalHitPosition);
		}

		/// <summary>
		/// Main method. Called when someone shoots.
		/// </summary>
		public static void Shoot(Frame f, in EntityRef e)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(e);
			var weaponConfig = f.WeaponConfigs.GetConfig(playerCharacter->CurrentWeapon.GameId);
			var transform = f.Unsafe.GetPointer<Transform2D>(e);
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			var aimDirection = bb->GetVector2(f, Constants.AIM_DIRECTION_KEY);
			// TODO: Migrate all this outside blackboard
			if (aimDirection == FPVector2.Zero)
			{
				if (!f.Unsafe.TryGetPointer<TopDownController>(e, out var kcc))
				{
					return;
				}
				else
				{
					aimDirection = kcc->AimDirection == FPVector2.Zero ? kcc->AimDirection.Normalized : kcc->MoveDirection.Normalized;
				}
				if (aimDirection == FPVector2.Zero) return;
			}
			var aimRotation = aimDirection.ToRotation();
			var cannonEndPosition = transform->Position + FPVector2.Rotate(playerCharacter->ProjectileSpawnOffset * 3, aimRotation);
			bool blocked = !QuantumHelpers.HasLineOfSight(f, transform->Position, cannonEndPosition, f.Context.TargetMapOnlyLayerMask, QueryOptions.HitStatics, out _);
			if(f.Unsafe.TryGetPointer<BotCharacter>(e, out var bot) && bot->Target.IsValid && bb->GetBoolean(f, Constants.IS_AIM_PRESSED_KEY))
			{
				if (bot->SharpShootNextShot)
				{
					bot->SharpShootNextShot = false;
					if (bot->TrySharpShoot(e, f, bot->Target, out var sharpDirection))
					{
						aimDirection = sharpDirection;
						bb->Set(f, Constants.AIM_DIRECTION_KEY, aimDirection);
					}
				}
			}
			
			var position = transform->Position + FPVector2.Rotate(playerCharacter->ProjectileSpawnOffset, aimRotation);
			var rangeStat = f.Unsafe.GetPointer<Stats>(e)->GetStatData(StatType.AttackRange).StatValue;
			playerCharacter->ReduceMag(f, e); //consume a shot from your magazine
			bb->Set(f, Constants.BURST_SHOT_COUNT, bb->GetFP(f, Constants.BURST_SHOT_COUNT) - 1);
			bb->Set(f, Constants.LAST_SHOT_AT, f.Time);
			f.Events.OnPlayerAttack(playerCharacter->Player, e, playerCharacter->CurrentWeapon, weaponConfig, aimDirection, rangeStat);
			if (weaponConfig.NumberOfShots == 1 || weaponConfig.IsMeleeWeapon)
			{
				CreateProjectile(f, e, rangeStat, aimDirection, position, weaponConfig, 0, blocked);
			}
			else
			{
				FP max = weaponConfig.MinAttackAngle;
				FP angleStep = weaponConfig.MinAttackAngle / (weaponConfig.NumberOfShots - 1);
				FP angle = -max/ FP._2;
				for (var x = 0; x < weaponConfig.NumberOfShots; x++)
				{
					var burstDirection = FPVector2.Rotate(aimDirection, angle * FP.Deg2Rad);
					cannonEndPosition = transform->Position + FPVector2.Rotate(playerCharacter->ProjectileSpawnOffset * 3, burstDirection.ToRotation());
					blocked = !QuantumHelpers.HasLineOfSight(f, transform->Position, cannonEndPosition, f.Context.TargetMapOnlyLayerMask, QueryOptions.HitStatics, out _);
					CreateProjectile(f, e, rangeStat, burstDirection, position, weaponConfig, (byte)x, blocked);
					angle += angleStep;
				}
			}
		}
	
		private static void CreateProjectile(Frame f, in EntityRef shooter, in FP range, in FPVector2 aimingDirection, FPVector2 projectileStartPosition, in QuantumWeaponConfig weaponConfig, byte shotNumber, bool blocked)
		{
			FP accuracyMod = FP._0;
			if(weaponConfig.MinAttackAngle > FP._0 && !weaponConfig.IsMeleeWeapon && !(weaponConfig.NumberOfShots > 1))
			{
				accuracyMod = f.WeaponConfigs.GetRandomBakedAccuracyAngle(f, weaponConfig.Id);
			}
			var shotDirection = FPVector2.Rotate(aimingDirection, accuracyMod * FP.Deg2Rad).Normalized;
			var directionPerTick = shotDirection * weaponConfig.AttackHitSpeed.AsInt * f.DeltaTime;
			var despawnTime = FP._0;
			if (weaponConfig.IsMeleeWeapon)
			{
				despawnTime = f.Time + FP._0_20;
				projectileStartPosition += shotDirection * range / FP._2;
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
				Blocked = blocked,
				Iteration = 0,
				TeamSource = (byte)f.Unsafe.GetPointer<Targetable>(shooter)->Team,
				ShotNumber = shotNumber
			};
			
			var projectileEntity = f.Create(f.FindAsset<EntityPrototype>(weaponConfig.BulletPrototype != null
				? weaponConfig.BulletPrototype.Id
				: f.AssetConfigs.DefaultBulletPrototype.Id));
			
			var transform = f.Unsafe.GetPointer<Transform2D>(projectileEntity);

			transform->Position = projectile.SpawnPosition + CAMERA_CORRECTION;
			
			var direction = projectile.Direction;
			transform->Rotation = direction.Normalized.ToRotation();
			
			f.Add(projectileEntity, projectile);
			
			// Only on verified
			f.Events.OnProjectileFired(projectileEntity, projectile);
			
			// Can be read from predicted 
			f.Events.OnProjectileFiredPredicted(projectileEntity, projectile);
		}
	}
}