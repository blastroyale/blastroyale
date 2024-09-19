using Photon.Deterministic;
using Quantum.Profiling;

namespace Quantum.Systems.Bots
{
	/// <summary>
	/// Extension for shooting behaviour
	/// </summary>
	public static unsafe class BotShooting
	{
		/// <summary>
		/// This determines how fast innacurate bots will move the aim line towards their randomized angle
		/// to create the effect of smoothly aiming
		/// </summary>
		private static readonly FP ACCURACY_LERP_TICK = FP._0_01;

		public static FP GetMaxWeaponRange(this ref BotCharacter bot, in EntityRef entity, PlayerCharacter* pc, Frame f)
		{
			if (pc->HasMeleeWeapon(f, entity))
			{
				return Stats.GetStat(f, entity, StatType.AttackRange);
			}

			return FPMath.Min(Stats.GetStat(f, entity, StatType.AttackRange), bot.MaxAimingRange);
		}

		/// <summary>
		/// Updates to keep aiming at the current target
		/// </summary>
		public static EntityRef UpdateAimTarget(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f)
		{
			if (ReviveSystem.IsKnockedOut(f, filter.Entity))
			{
				return EntityRef.None;
			}

			if (filter.BotCharacter->BehaviourType == BotBehaviourType.StaticShooting) return EntityRef.None;

			var target = filter.BotCharacter->Target;

			if (target != EntityRef.None)
			{
				// We need to check also for AlivePlayerCharacter because with respawns we don't destroy Player Entities
				if (QuantumHelpers.IsDestroyed(f, target) || !f.Has<AlivePlayerCharacter>(target))
				{
					BotLogger.LogAction(ref filter, "Target is not valid, stopping");
					filter.ClearTarget(f);
				}
				// Aim at target
				else
				{
					var weaponConfig = f.WeaponConfigs.GetConfig(filter.PlayerCharacter->CurrentWeapon.GameId);
					var maxRange = filter.BotCharacter->GetMaxWeaponRange(filter.Entity, filter.PlayerCharacter, f);
					var team = f.Unsafe.GetPointer<Targetable>(filter.Entity)->Team;
					if (filter.TryToAimAtEnemy(f, team, maxRange, target, out var targetHit))
					{
						var speedUpMutatorExists = f.Context.Mutators.HasFlagFast(Mutator.SpeedUp);
						var speed = f.Unsafe.GetPointer<Stats>(filter.Entity)->Values[(int)StatType.Speed].StatValue;
						speed *= filter.BotCharacter->MovementSpeedMultiplier;
						speed *= weaponConfig.AimingMovementSpeed;
						var kcc = f.Unsafe.GetPointer<TopDownController>(filter.Entity);
						kcc->MaxSpeed = speedUpMutatorExists ? speed * Constants.MUTATOR_SPEEDUP_AMOUNT : speed;
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

		public struct BotTargetFilter
		{
			public EntityRef Entity;
			public Targetable* Transform;
		}

		// We loop through targetable entities trying to find if any is eligible to shoot at
		public static void FindEnemiesToShootAt(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f)
		{
			if (ReviveSystem.IsKnockedOut(f, botFilter.Entity))
			{
				return;
			}

			if (botFilter.BotCharacter->BehaviourType == BotBehaviourType.StaticShooting) return;

			var target = EntityRef.None;
			// We do line/shapecasts for enemies in sight
			// If there is a target in Sight then store this Target into the blackboard variable
			// We check enemies one by one until we find a valid enemy in sight
			// Note: Bots against bots use the full weapon range
			// TODO: Select not a random, but the closest possible enemy to shoot at
			var limitedTargetRange = botFilter.BotCharacter->GetMaxWeaponRange(botFilter.Entity, botFilter.PlayerCharacter, f);
			var team = f.Unsafe.GetPointer<Targetable>(botFilter.Entity)->Team;

			var it = f.Unsafe.FilterStruct<BotTargetFilter>();
			it.UseCulling = true;
			var filter = default(BotTargetFilter);

			while (it.Next(&filter))
			{
				if (botFilter.TryToAimAtEnemy(f, team, limitedTargetRange, filter.Entity, out var targetHit))
				{
					target = targetHit;
					break;
				}
			}

			if (target.IsValid && botFilter.BotCharacter->Target != target)
			{
				botFilter.SetAttackTarget(f, target);
			}

			botFilter.SetSearchForEnemyDelay(f);
		}

		public static bool IsGoingTowardsEnemy(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f)
		{
			return botFilter.BotCharacter->MoveTarget.IsValid
				&& botFilter.BotCharacter->MoveTarget != botFilter.Entity
				&& f.Has<PlayerCharacter>(botFilter.BotCharacter->MoveTarget);
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

			// if i did had a target but combat is over
			if (botFilter.BotCharacter->Target.IsValid)
			{
				// if im not going towards a valid collectible ill re-think my life
				if (!botFilter.IsGoingTowardsEnemy(f) && !botFilter.IsGoingTowardsValidCollectible(f, out _))
				{
					botFilter.BotCharacter->ResetTargetWaypoint(f);
					botFilter.NavMeshAgent->Stop(f, botFilter.Entity, true);
				}
			}

			botFilter.BotCharacter->Target = EntityRef.None;
		}

		public static void StopAiming(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f)
		{
			var speed = f.Unsafe.GetPointer<Stats>(botFilter.Entity)->Values[(int)StatType.Speed].StatValue;
			speed *= botFilter.BotCharacter->MovementSpeedMultiplier;

			var speedUpMutatorExists = f.Context.Mutators.HasFlagFast(Mutator.SpeedUp);
			speed = speedUpMutatorExists ? speed * Constants.MUTATOR_SPEEDUP_AMOUNT : speed;

			ReviveSystem.OverwriteMaxMoveSpeed(f, botFilter.Entity, ref speed);
			// When we clear the target we also return speed to normal
			// because without a target bots don't shoot
			f.Unsafe.GetPointer<NavMeshSteeringAgent>(botFilter.Entity)->MaxSpeed = speed;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botFilter.Entity);
			bb->Set(f, Constants.IS_AIM_PRESSED_KEY, false);

			BotLogger.LogAction(ref botFilter, "[Aim] Cleared Aim");
		}

		public static void StopAiming(Frame f, BotCharacter* botCharacter, EntityRef entity)
		{
			var speed = f.Unsafe.GetPointer<Stats>(entity)->Values[(int)StatType.Speed].StatValue;
			speed *= botCharacter->MovementSpeedMultiplier;

			var speedUpMutatorExists = f.Context.Mutators.HasFlagFast(Mutator.SpeedUp);
			speed = speedUpMutatorExists ? speed * Constants.MUTATOR_SPEEDUP_AMOUNT : speed;

			ReviveSystem.OverwriteMaxMoveSpeed(f, entity, ref speed);
			// When we clear the target we also return speed to normal
			// because without a target bots don't shoot
			f.Unsafe.GetPointer<NavMeshSteeringAgent>(entity)->MaxSpeed = speed;

			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			bb->Set(f, Constants.IS_AIM_PRESSED_KEY, false);
			bb->Set(f, Constants.IS_SHOOTING_KEY, false);

			BotLogger.LogAction(entity, "[Aim] Cleared Aim");
		}

		/// <summary>
		/// Tries basic combat movement.
		/// Will attempt not to get close to the enemy and stay in a decent range while moving randomly.
		/// </summary>
		public static bool TryCombatMovement(this ref BotCharacter bot, in EntityRef e, Frame f, in FPVector2 botPosition, in EntityRef target, in FPVector2 targetPosition, in FP rangeSquared, in FP distanceSquared)
		{
			if (bot.IsLowLife(e, f) || bot.IsStaticMovement()) return false;
			if (!bot.Target.IsValid && bot.HasWaypoint(e, f)) return false; // if im not combating and moving ill ignore
			if (!bot.GetCanTakeDecision(f)) return false;

			// If bot is too close he will walk randomly around the bot itself 
			var minCombatDistance = rangeSquared * FP._0_33;

			if (distanceSquared < minCombatDistance)
			{
				if (bot.WanderInsideCircle(e, f, botPosition, minCombatDistance))
				{
					BotLogger.LogAction(e, "Moving away from target, too close to him");
					bot.SetHasWaypoint(e, f);
					bot.SetNextDecisionDelay(f, FP._1);
					bot.SetSearchForEnemyDelay(f);
					return true;
				}
			}

			// If im engaging already and im inside weapon range
			if (distanceSquared <= rangeSquared && bot.Target == target)
			{
				// If im getting my ass kicked i wont try to do combat movement ill god damn run
				if (Stats.VitalityRatio(e, f) + FP._0_50 < Stats.VitalityRatio(bot.Target, f))
				{
					return false;
				}

				if (bot.WanderInsideCircle(e, f, targetPosition, rangeSquared))
				{
					BotLogger.LogAction(e, "Moving away from target, too close to him");
					bot.SetHasWaypoint(e, f);
					bot.SetNextDecisionDelay(f, FP._1);
					bot.SetSearchForEnemyDelay(f);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Will attempt to predict where the target will be given his movement speed
		/// and will attempt to shoot where the target will be and not where the target is now
		/// this means this should be a super precise shot that the player will needs to react to
		///
		/// THe idea is simple:
		/// - Check how many bot weapon moves per frame
		/// - Check how much the target moves per frame in current direction
		/// - Given distance between target and the bot, measure how many frames it will take the bullet to hit
		/// - Calculate a predicted offset where the target will be, given current velocity, after those frames
		/// - Fire at that place
		///
		/// Maybe theres some fancy trigonometry that could solve this better, but i suck at it so made it dummy way.
		/// </summary>
		public static bool TrySharpShoot(this ref BotCharacter bot, in EntityRef botEntity, Frame f, in EntityRef target, out FPVector2 direction)
		{
			direction = default;
			if (!f.Has<AlivePlayerCharacter>(target)) return false;
			var character = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
			var weaponConfig = f.WeaponConfigs.GetConfig(character->CurrentWeapon.GameId);
			var weaponTraversePerFrame = weaponConfig.AttackHitSpeed.AsInt * f.DeltaTime;
			if (weaponTraversePerFrame == FP._0) return false;
			var kcc = f.Unsafe.GetPointer<TopDownController>(target);
			var targetPosition = target.GetPosition(f);
			var botPosition = botEntity.GetPosition(f);
			var currentDistance = FPVector2.Distance(botPosition, targetPosition);
			var estimationFramesToHit = currentDistance / weaponTraversePerFrame;
			var moveSpeed = kcc->Velocity;
			moveSpeed = moveSpeed.Normalized * kcc->MaxSpeed;
			var estimatedPositionOffset = moveSpeed * f.DeltaTime * estimationFramesToHit;
			targetPosition += estimatedPositionOffset;
			direction = (targetPosition - botPosition);
			return true;
		}

		public static bool TrySwitchToHammer(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f)
		{
			botFilter.PlayerCharacter->EquipSlotWeapon(f, botFilter.Entity, Constants.WEAPON_INDEX_DEFAULT);
			return true;
		}

		// We check specials and try to use them depending on their type if possible
		public static bool TryUseSpecials(this ref BotCharacter bot, PlayerInventory* inventory, EntityRef botEntity, Frame f)
		{
			// If there are no specials in this match, then no need to try using them as bots don't have them
			if (f.Context.Mutators.HasFlagFast(Mutator.DoNotDropSpecials))
			{
				return false;
			}

			if (f.Time < bot.NextAllowedSpecialUseTime)
			{
				return false;
			}

			if (!(f.RNG->Next() < bot.ChanceToUseSpecial))
			{
				return false;
			}

			for (var i = 0; i <= 1; i++)
			{
				if (TryUseSpecial(f, inventory, i, botEntity, bot.Target))
				{
					bot.NextAllowedSpecialUseTime = f.Time + f.RNG->NextInclusive(bot.SpecialCooldown);

					return true;
				}
			}

			return false;
		}

		public static bool TryUseSpecial(Frame f, PlayerInventory* inventory, int specialIndex, EntityRef entity,
										 EntityRef target)
		{
			var special = inventory->Specials[specialIndex];

			if ((target != EntityRef.None || special.SpecialType == SpecialType.ShieldSelfStatus) &&
				special.TryActivate(f, PlayerRef.None, entity, FPVector2.Zero, specialIndex))
			{
				return true;
			}

			return false;
		}

		public static bool TryToAimAtEnemy(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f, int team,
										   in FP targetRange, in EntityRef targetToCheck, out EntityRef targetHit)
		{
			return botFilter.BotCharacter->TryToAimAtEnemy(botFilter.Entity, f, team, targetRange, targetToCheck,
				out targetHit);
		}

		// We check specific entity if a bot can hit it or not, to make a decision to aim or not to aim
		// Note that as a result we can get another entity that is being hit, for instance if it appears between the bot and a target that we are checking
		public static bool TryToAimAtEnemy(this ref BotCharacter bot, EntityRef botEntity, Frame f, int team, in FP targetRange, in EntityRef targetToCheck, out EntityRef targetHit)
		{
			targetHit = EntityRef.None;
			if (botEntity == targetToCheck) return false;

			if (!VisibilityAreaSystem.CanEntityViewEntityRaw(f, botEntity, targetToCheck))
			{
				//BotLogger.LogAction(botEntity, "Cant view "+targetToCheck);
				return false;
			}

			if (!QuantumHelpers.IsAttackable(f, targetToCheck, team))
			{
				//BotLogger.LogAction(botEntity, "Not attackable "+targetToCheck);
				return false;
			}

			var distanceSquared = QuantumHelpers.GetDistance(f, botEntity, targetToCheck);
			var maxRangeSquared = targetRange * targetRange;
			if (distanceSquared > maxRangeSquared)
			{
				//BotLogger.LogAction(botEntity, "Not in range "+targetToCheck);
				return false;
			}

			var botPosition = botEntity.GetPosition(f);

			var targetPosition = f.Unsafe.GetPointer<Transform2D>(targetToCheck)->Position;

			if (bot.TryCombatMovement(botEntity, f, botPosition, targetToCheck, targetPosition, maxRangeSquared, distanceSquared))
			{
				targetHit = targetToCheck;
			}
			else
			{
				var hit = f.Physics2D.Linecast(botPosition,
					targetPosition,
					f.Context.TargetPlayerLineOfSightLayerMask, QueryOptions.HitStatics |
					QueryOptions.HitKinematics);

				if (!hit.HasValue)
				{
					targetHit = targetToCheck;
				}
			}

			if (targetHit.IsValid)
			{
				bot.SetAimWithAccuracyLerp(botEntity, f, targetHit, targetPosition);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Performs an accuracy lerp so bot rotates while pretending to be innacurate
		/// This means players can see the bot innacuracy rotation and react to it.
		///
		/// We reusing player character blackboard components here to save space. Those blackboard
		/// attributes are only being used by real players.
		/// </summary>
		private static void SetAimWithAccuracyLerp(this ref BotCharacter bot, in EntityRef botEntity, Frame f, in EntityRef target, in FPVector2 targetPosition)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botEntity);
			var botPosition = botEntity.GetPosition(f);
			var accuracy = bb->GetFP(f, Constants.ACCURACY_LERP);
			var perfectAim = (targetPosition - botPosition).Normalized;
			if (accuracy > -ACCURACY_LERP_TICK && accuracy < ACCURACY_LERP_TICK)
			{
				var spread = (bot.AccuracySpreadAngle * FP.Deg2Rad);
				accuracy = f.RNG->Next(-spread, spread);
			}
			else
			{
				if (accuracy > 0) accuracy -= ACCURACY_LERP_TICK;
				else accuracy += ACCURACY_LERP_TICK;
			}

			perfectAim = (perfectAim.ToRotation() + accuracy).ToDirection();
			bb->Set(f, Constants.AIM_DIRECTION_KEY, perfectAim);
			bb->Set(f, Constants.ACCURACY_LERP, accuracy);
		}
	}
}