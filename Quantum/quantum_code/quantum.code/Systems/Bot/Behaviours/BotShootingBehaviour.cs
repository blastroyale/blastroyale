using Photon.Deterministic;

namespace Quantum.Systems.Bot
{
	public unsafe class BotShootingBehaviour : BotBehaviour
	{
		private BotMovementBehaviour _botMovementBehaviour;

		public BotShootingBehaviour(BotMovementBehaviour botMovementBehaviour)
		{
			_botMovementBehaviour = botMovementBehaviour;
		}

		public override bool OnEveryUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			bool isDecisionTime = IsDecisionTime(f, ref filter);
			if (isDecisionTime)
			{
				CheckChangeWeapon(f, ref filter);
			}

			CheckChangeWeapon(f, ref filter);
			if (AimAtTarget(f, ref filter))
			{
				// Is Shooting
				return true;
			}

			TryToFindTarget(f, ref filter);

			if (isDecisionTime)
			{
				TryUseSpecials(f, ref filter);
			}

			return false;
		}

		private void CheckChangeWeapon(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			// In case a bot has a gun and ammo but switched to a hammer - we switch back to a gun
			if (filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) &&
				filter.PlayerCharacter->GetAmmoAmountFilled(f, filter.Entity) > FP._0)
			{
				for (var slotIndex = 1; slotIndex < filter.PlayerCharacter->WeaponSlots.Length; slotIndex++)
				{
					if (filter.PlayerCharacter->WeaponSlots[slotIndex].Weapon.IsValid())
					{
						filter.PlayerCharacter->EquipSlotWeapon(f, filter.Entity, slotIndex);
						break;
					}
				}
			}
		}

		private void TryToFindTarget(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			// Bots look for others to shoot at not on every frame
			if (f.Time > filter.BotCharacter->NextLookForTargetsToShootAtTime)
			{
				CheckEnemiesToShooAt(f, ref filter);

				filter.BotCharacter->NextLookForTargetsToShootAtTime =
					f.Time + filter.BotCharacter->LookForTargetsToShootAtInterval;
			}
		}


		private bool AimAtTarget(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			// If a bot has a valid target then we correct the bot's speed according
			// to the weapon they carry and turn the bot towards the target
			// otherwise we return speed to normal and let automatic navigation turn the bot
			var target = filter.BotCharacter->Target;

			if (target == EntityRef.None)
			{
				return false;
			}

			var speed = f.Get<Stats>(filter.Entity).Values[(int) StatType.Speed].StatValue;
			speed *= filter.BotCharacter->MovementSpeedMultiplier;
			var weaponConfig = WeaponConfig(f, ref filter);


			// We need to check also for AlivePlayerCharacter because with respawns we don't destroy Player Entities
			if (QuantumHelpers.IsDestroyed(f, target) || !f.Has<AlivePlayerCharacter>(target))
			{
				ClearTarget(f, ref filter);
				return false;
			}


			// Aim at target
			var weaponTargetRange = FPMath.Min(f.Get<Stats>(filter.Entity).GetStatData(StatType.AttackRange).StatValue, filter.BotCharacter->MaxAimingRange);
			var botPosition = filter.Transform->Position;
			var team = f.Get<Targetable>(filter.Entity).Team;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);

			botPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

			if (TryToAimAtEnemy(f, ref filter, botPosition, team, weaponTargetRange, target, out var targetHit))
			{
				var speedUpMutatorExists =
					f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
				speed *= weaponConfig.AimingMovementSpeed;

				var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);
				kcc->MaxSpeed = speedUpMutatorExists ? speed * speedUpMutatorConfig.Param1 : speed;

				filter.BotCharacter->Target = targetHit;
				QuantumHelpers.LookAt2d(f, filter.Entity, targetHit, FP._0);
				bb->Set(f, Constants.IsAimPressedKey, true);
				return true;
			}
			// Clear target if can't aim at it
			else
			{
				ClearTarget(f, ref filter);
			}

			return false;
		}

		// We check specific entity if a bot can hit it or not, to make a decision to aim or not to aim
		// Note that as a result we can get another entity that is being hit, for instance if it appears between the bot and a target that we are checking
		private bool TryToAimAtEnemy(Frame f, ref BotCharacterSystem.BotCharacterFilter filter, FPVector3 botPosition, int team,
									 FP targetRange, EntityRef targetToCheck, out EntityRef targetHit)
		{
			targetHit = EntityRef.None;

			if (!VisibilityAreaSystem.CanEntityViewEntity(f, filter.Entity, targetToCheck).CanSee)
			{
				return false;
			}

			if (!QuantumHelpers.IsAttackable(f, targetToCheck, team) ||
				!QuantumHelpers.IsEntityInRange(f, filter.Entity, targetToCheck, FP._0, targetRange))
			{
				return false;
			}

			var targetPosition = f.Get<Transform3D>(targetToCheck).Position;
			targetPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

			var hit = f.Physics3D.Linecast(botPosition,
				targetPosition,
				f.Context.TargetAllLayerMask,
				QueryOptions.HitDynamics | QueryOptions.HitStatics |
				QueryOptions.HitKinematics);

			// TODO: Ideally we shouldn't check "hit.Value.Entity != EntityRef.None" because layers should solve it,
			// however sometimes we have a hit.HasValue but hit.Value.Entity is EntityRef.None which means we hit something that is not an Entity
			if (hit.HasValue && hit.Value.Entity != EntityRef.None)
			{
				var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);

				targetHit = hit.Value.Entity;

				// Apply bots inaccuracy
				var aimDirection = (targetPosition - botPosition).XZ;
				if (filter.BotCharacter->AccuracySpreadAngle > 0)
				{
					var angleHalfInRad = (filter.BotCharacter->AccuracySpreadAngle * FP.Deg2Rad) / FP._2;
					aimDirection = FPVector2.Rotate(aimDirection, f.RNG->Next(-angleHalfInRad, angleHalfInRad));
				}

				bb->Set(f, Constants.AimDirectionKey, aimDirection);

				return true;
			}

			return false;
		}


		private void StopAiming(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			_botMovementBehaviour.UpdateMovementSpeed(f, ref filter);
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);
			bb->Set(f, Constants.IsAimPressedKey, false);
		}

		private void ClearTarget(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			StopAiming(f, ref filter);

			// If the bot was moving towards this enemy then we clear move target and force a bot to make a decision
			// if (filter.BotCharacter->MoveTarget == filter.BotCharacter->Target)
			// {
			// 	filter.BotCharacter->MoveTarget = EntityRef.None;
			// 	filter.NavMeshAgent->Stop(f, filter.Entity, true);
			// }

			filter.BotCharacter->Target = EntityRef.None;
		}

		// We loop through targetable entities trying to find if any is eligible to shoot at
		private void CheckEnemiesToShooAt(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			var target = EntityRef.None;


			// We do line/shapecasts for enemies in sight
			// If there is a target in Sight then store this Target into the blackboard variable
			// We check enemies one by one until we find a valid enemy in sight
			// TODO: Select not a random, but the closest possible enemy to shoot at
			var weaponTargetRange = f.Get<Stats>(filter.Entity).GetStatData(StatType.AttackRange).StatValue;
			var limitedTargetRange = FPMath.Min(weaponTargetRange, filter.BotCharacter->MaxAimingRange);
			var botPosition = filter.Transform->Position;
			var team = f.Get<Targetable>(filter.Entity).Team;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);

			botPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

			foreach (var targetCandidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
			{
				if (TryToAimAtEnemy(f, ref filter, botPosition, team, limitedTargetRange,
						targetCandidate.Entity, out var targetHit))
				{
					target = targetHit;
					break;
				}
			}

			filter.BotCharacter->Target = target;

			bb->Set(f, Constants.IsAimPressedKey, target != EntityRef.None);
		}

		private QuantumWeaponConfig WeaponConfig(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			return f.WeaponConfigs.GetConfig(filter.PlayerCharacter->CurrentWeapon.GameId);
		}


		// We check specials and try to use them depending on their type if possible
		private bool TryUseSpecials(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			if (!(f.RNG->Next() < filter.BotCharacter->ChanceToUseSpecial))
			{
				return false;
			}

			if (TryUseSpecial(f, filter.PlayerCharacter, 0, filter.Entity, filter.BotCharacter->Target))
			{
				return true;
			}

			if (TryUseSpecial(f, filter.PlayerCharacter, 1, filter.Entity, filter.BotCharacter->Target))
			{
				return true;
			}

			return false;
		}

		private bool TryUseSpecial(Frame f, PlayerCharacter* playerCharacter, int specialIndex, EntityRef entity,
								   EntityRef target)
		{
			var special = playerCharacter->WeaponSlot->Specials[specialIndex];

			if ((target != EntityRef.None || special.SpecialType == SpecialType.ShieldSelfStatus) &&
				special.TryActivate(f, PlayerRef.None, entity, FPVector2.Zero, specialIndex))
			{
				playerCharacter->WeaponSlot->Specials[specialIndex] = special;
				return true;
			}

			return false;
		}
	}
}