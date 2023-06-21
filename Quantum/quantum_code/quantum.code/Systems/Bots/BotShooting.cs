using Photon.Deterministic;

namespace Quantum.Systems.Bots
{
	/// <summary>
	/// Extension for shooting behaviour
	/// </summary>
	public static unsafe class BotShooting
	{

		/// <summary>
		/// Updates to keep aiming at the current target
		/// </summary>
		public static EntityRef UpdateAimTarget(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f)
		{
			var target = filter.BotCharacter->Target;
			var weaponConfig = f.WeaponConfigs.GetConfig(filter.PlayerCharacter->CurrentWeapon.GameId);

			if (target != EntityRef.None)
			{
				// We need to check also for AlivePlayerCharacter because with respawns we don't destroy Player Entities
				if (QuantumHelpers.IsDestroyed(f, target) || !f.Has<AlivePlayerCharacter>(target))
				{
					filter.ClearTarget(f);
				}
				// Aim at target
				else
				{
					var weaponTargetRange =
						FPMath.Min(f.Get<Stats>(filter.Entity).GetStatData(StatType.AttackRange).StatValue,
							filter.BotCharacter->MaxAimingRange);
					var botPosition = filter.Transform->Position;
					var team = f.Get<Targetable>(filter.Entity).Team;
	
					botPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

					if (filter.TryToAimAtEnemy(f, botPosition, team, weaponTargetRange, target, out var targetHit))
					{
						var speedUpMutatorExists =
							f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
						var speed = f.Get<Stats>(filter.Entity).Values[(int)StatType.Speed].StatValue;
						speed *= filter.BotCharacter->MovementSpeedMultiplier;
						speed *= weaponConfig.AimingMovementSpeed;
						var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);
						kcc->MaxSpeed = speedUpMutatorExists ? speed * speedUpMutatorConfig.Param1 : speed;
						filter.SetAttackTarget(f, targetHit);
						target = targetHit;
					}
					// Clear target if can't aim at it
					else
					{
						filter.ClearTarget(f);
					}
				}
			}
			return target;
		}
		// We loop through targetable entities trying to find if any is eligible to shoot at
		public static void CheckEnemiesToShooAt(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f, ref QuantumWeaponConfig weaponConfig)
		{
			var target = EntityRef.None;

			// We do line/shapecasts for enemies in sight
			// If there is a target in Sight then store this Target into the blackboard variable
			// We check enemies one by one until we find a valid enemy in sight
			// Note: Bots against bots use the full weapon range
			// TODO: Select not a random, but the closest possible enemy to shoot at
			var weaponTargetRange = f.Get<Stats>(botFilter.Entity).GetStatData(StatType.AttackRange).StatValue;
			var limitedTargetRange = FPMath.Min(weaponTargetRange, botFilter.BotCharacter->MaxAimingRange);
			var botPosition = botFilter.Transform->Position;
			var team = f.Get<Targetable>(botFilter.Entity).Team;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botFilter.Entity);

			botPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

			foreach (var targetCandidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
			{
				if (botFilter.TryToAimAtEnemy(f, botPosition, team, limitedTargetRange,
						targetCandidate.Entity, out var targetHit))
				{
					target = targetHit;
					break;
				}
			}

			if (target.IsValid && botFilter.BotCharacter->Target != target)
			{
				botFilter.SetAttackTarget(f, target);
			}
			else if(!target.IsValid)
			{
				botFilter.ClearTarget(f);
			}
		}
		
		public static void ClearTarget(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f)
		{
			botFilter.StopAiming(f);

			// If the bot was moving towards this enemy then we clear move target and force a bot to make a decision
			// if (filter.BotCharacter->MoveTarget == filter.BotCharacter->Target)
			// {
			// 	filter.BotCharacter->MoveTarget = EntityRef.None;
			// 	filter.NavMeshAgent->Stop(f, filter.Entity, true);
			// }

			botFilter.BotCharacter->Target = EntityRef.None;
		}
		
		public static void StopAiming(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f)
		{
			var speed = f.Get<Stats>(botFilter.Entity).Values[(int)StatType.Speed].StatValue;
			speed *= botFilter.BotCharacter->MovementSpeedMultiplier;

			var speedUpMutatorExists = f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
			speed = speedUpMutatorExists ? speed * speedUpMutatorConfig.Param1 : speed;

			// When we clear the target we also return speed to normal
			// because without a target bots don't shoot
			f.Unsafe.GetPointer<CharacterController3D>(botFilter.Entity)->MaxSpeed = speed;

			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botFilter.Entity);
			bb->Set(f, Constants.IsAimPressedKey, false);
		}

		// We check specific entity if a bot can hit it or not, to make a decision to aim or not to aim
		// Note that as a result we can get another entity that is being hit, for instance if it appears between the bot and a target that we are checking
		public static bool TryToAimAtEnemy(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f, FPVector3 botPosition, int team,
																																	FP targetRange, EntityRef targetToCheck, out EntityRef targetHit)
		{
			targetHit = EntityRef.None;

			if (!VisibilityAreaSystem.CanEntityViewEntity(f, botFilter.Entity, targetToCheck).CanSee)
			{
				return false;
			}

			if (!QuantumHelpers.IsAttackable(f, targetToCheck, team) ||
				!QuantumHelpers.IsEntityInRange(f, botFilter.Entity, targetToCheck, FP._0, targetRange))
			{
				return false;
			}

			var targetPosition = f.Get<Transform3D>(targetToCheck).Position;
			targetPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;


			// Are bots inside eachother
			if (FPVector3.DistanceSquared(botPosition, targetPosition) < FP._0_20)
			{
				var random = FPVector3.Normalize(botPosition - targetPosition) * f.RNG->NextInclusive(FP._1, FP._3);
				var randomPosition = botPosition + random;
				if (QuantumHelpers.SetClosestTarget(f, botFilter.Entity, randomPosition))
				{
					targetHit = targetToCheck;
					return true;
				}
			}

			var hit = f.Physics3D.Linecast(botPosition,
				targetPosition,
				f.Context.TargetAllLayerMask,
				QueryOptions.HitDynamics | QueryOptions.HitStatics |
				QueryOptions.HitKinematics);


			// TODO: Ideally we shouldn't check "hit.Value.Entity != EntityRef.None" because layers should solve it,
			// however sometimes we have a hit.HasValue but hit.Value.Entity is EntityRef.None which means we hit something that is not an Entity
			if (hit.HasValue && hit.Value.Entity != EntityRef.None)
			{
				var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botFilter.Entity);

				targetHit = hit.Value.Entity;

				// Apply bots inaccuracy
				var aimDirection = (targetPosition - botPosition).XZ;
				if (botFilter.BotCharacter->AccuracySpreadAngle > 0)
				{
					var angleHalfInRad = (botFilter.BotCharacter->AccuracySpreadAngle * FP.Deg2Rad) / FP._2;
					aimDirection = FPVector2.Rotate(aimDirection, f.RNG->Next(-angleHalfInRad, angleHalfInRad));
				}

				bb->Set(f, Constants.AimDirectionKey, aimDirection);

				return true;
			}

			return false;
		}

	}
}