using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Photon.Deterministic;
using Quantum.Systems.Bots;
using static Quantum.RngSessionExtension;

namespace Quantum.Systems.Bots
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="BotCharacter"/>
	/// </summary>
	public unsafe class BotCharacterSystem : SystemMainThreadFilter<BotCharacterSystem.BotCharacterFilter>,
											 ISignalAllPlayersSpawned, ISignalOnNavMeshWaypointReached, ISignalOnNavMeshSearchFailed, ISignalOnComponentRemoved<BotCharacter>
	{
		private BotSetup _botSetup = new BotSetup();

		public struct BotCharacterFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public BotCharacter* BotCharacter;
			public PlayerCharacter* PlayerCharacter;
			public AlivePlayerCharacter* AlivePlayerCharacter;
			public NavMeshPathfinder* NavMeshAgent;
		}

		/// <inheritdoc />
		public void AllPlayersSpawned(Frame f)
		{
			var players = f.GetAllPlayerDatas();
			if (!players.Any())
			{
				return; // no players no game
			}

			var averagePlayerTrophies = Convert.ToUInt32(
				Math.Round(
					players
						.Average(p => p.PlayerTrophies)));
			_botSetup.InitializeBots(f, averagePlayerTrophies);
		}


		/// <inheritdoc />
		public override void Update(Frame f, ref BotCharacterFilter filter)
		{
			// If it's a deathmatch game mode and a bot is dead then we process respawn behaviour
			if (f.Context.GameModeConfig.BotRespawn && f.TryGet<DeadPlayerCharacter>(filter.Entity, out var deadBot))
			{
				// If the bot is dead and it's not yet the time to respawn then we skip the update
				if (f.Time < deadBot.TimeOfDeath + f.GameConfig.PlayerRespawnTime)
				{
					return;
				}

				var agent = f.Unsafe.GetPointer<HFSMAgent>(filter.Entity);
				HFSMManager.TriggerEvent(f, &agent->Data, filter.Entity, Constants.RespawnEvent);
			}

			// If a bot is not alive OR a bot is stunned 
			// then we don't go further with the behaviour
			if (!f.Has<AlivePlayerCharacter>(filter.Entity) || f.Has<Stun>(filter.Entity) ||
				// Hack to prevent the bots to act while player's camera animation is still playing
				f.Time < f.GameConfig.PlayerRespawnTime)
			{
				return;
			}

			var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);

			if (!filter.BotCharacter->SpeedResetAfterLanding)
			{
				filter.BotCharacter->SpeedResetAfterLanding = true;

				// We call stop aiming once here to set the movement speed to a proper stat value
				StopAiming(f, ref filter);
			}

			// Distribute bot processing in 15 frames
			if (filter.BotCharacter->BotNameIndex % 15 == f.Number % 15)
			{
				return;
			}


			if (!kcc->Grounded)
			{
				kcc->Move(f, filter.Entity, FPVector3.Zero);
				return;
			}


			var circleCenter = FPVector2.Zero;
			var circleRadius = FP._0;
			var circleIsShrinking = false;
			var circleTargetCenter = FPVector2.Zero;
			var circleTargetRadius = FP._0;
			var circleTimeToShrink = FP._0;
			if (f.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				circle.GetMovingCircle(f, out circleCenter, out circleRadius);
				circleIsShrinking = circle.ShrinkingStartTime <= f.Time;
				circleTargetCenter = circle.TargetCircleCenter;
				circleTargetRadius = circle.TargetRadius;
				circleTimeToShrink = circle.ShrinkingStartTime - f.Time;
			}

			bool isTakingCircleDamage = filter.AlivePlayerCharacter->TakingCircleDamage;


			// If a bot has a valid target then we correct the bot's speed according
			// to the weapon they carry and turn the bot towards the target
			// otherwise we return speed to normal and let automatic navigation turn the bot
			var target = filter.BotCharacter->Target;
			var weaponConfig = f.WeaponConfigs.GetConfig(filter.PlayerCharacter->CurrentWeapon.GameId);

			if (target != EntityRef.None)
			{
				// We need to check also for AlivePlayerCharacter because with respawns we don't destroy Player Entities
				if (QuantumHelpers.IsDestroyed(f, target) || !f.Has<AlivePlayerCharacter>(target) || isTakingCircleDamage)
				{
					ClearTarget(f, ref filter);
				}
				// Aim at target
				else
				{
					var weaponTargetRange =
						FPMath.Min(f.Get<Stats>(filter.Entity).GetStatData(StatType.AttackRange).StatValue,
							filter.BotCharacter->MaxAimingRange);
					var botPosition = filter.Transform->Position;
					var team = f.Get<Targetable>(filter.Entity).Team;
					var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);

					botPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

					if (TryToAimAtEnemy(f, ref filter, botPosition, team, weaponTargetRange, target, out var targetHit))
					{
						var speedUpMutatorExists =
							f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
						var speed = f.Get<Stats>(filter.Entity).Values[(int)StatType.Speed].StatValue;
						speed *= filter.BotCharacter->MovementSpeedMultiplier;
						speed *= weaponConfig.AimingMovementSpeed;

						kcc->MaxSpeed = speedUpMutatorExists ? speed * speedUpMutatorConfig.Param1 : speed;

						filter.BotCharacter->Target = targetHit;
						QuantumHelpers.LookAt2d(f, filter.Entity, targetHit, FP._0);
						bb->Set(f, Constants.IsAimPressedKey, true);
						target = targetHit;
					}
					// Clear target if can't aim at it
					else
					{
						ClearTarget(f, ref filter);
					}
				}
			}

			// Bots look for others to shoot at not on every frame
			if (f.Time > filter.BotCharacter->NextLookForTargetsToShootAtTime)
			{
				CheckEnemiesToShooAt(f, ref filter, ref weaponConfig);

				filter.BotCharacter->NextLookForTargetsToShootAtTime =
					f.Time + filter.BotCharacter->LookForTargetsToShootAtInterval;
			}

			// Static bots don't move so no need to process anything else
			if (filter.BotCharacter->BehaviourType == BotBehaviourType.Dumb ||
				filter.BotCharacter->BehaviourType == BotBehaviourType.Static)
			{
				return;
			}

			// Check move target in case it disappeared or a bot collected it and needs to move on
			// We set 0.5 second delay before letting a bot making another decision
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				QuantumHelpers.IsDestroyed(f, filter.BotCharacter->MoveTarget))
			{
				filter.BotCharacter->MoveTarget = EntityRef.None;
				filter.NavMeshAgent->Stop(f, filter.Entity, true);
				filter.BotCharacter->NextDecisionTime = f.Time + FP._0_05;
			}

			if (filter.BotCharacter->TeamSize > 1)
			{
				CheckOnTeammates(f, ref filter);
			}

			if (isTakingCircleDamage)
			{
				GoToCenterOfCircle(f, ref filter, circleCenter);
				LogAction(ref filter, "go center of circle because of damage");
				return;
			}

			// Do not do any decision making if the time has not come, unless a bot have no target to move to
			if (filter.BotCharacter->MoveTarget != EntityRef.None
				&& f.Time < filter.BotCharacter->NextDecisionTime)
			{
				LogAction(ref filter, "waiting for decision " + (filter.BotCharacter->NextDecisionTime - f.Time));

				return;
			}

			///////////////////////////////////////////////////////////
			// The following code works in Decision time intervals
			///////////////////////////////////////////////////////////


			// If bot is collecting something at the moment then let this bot finish collection before doing anything else
			// and also clear StuckPosition so bot doesn't think that they stuck
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				f.TryGet<Collectable>(filter.BotCharacter->MoveTarget, out var collectable) &&
				collectable.IsCollecting(filter.PlayerCharacter->Player))
			{
				LogAction(ref filter, "is collecting item");
				filter.BotCharacter->NextDecisionTime = collectable.CollectorsEndTime[filter.PlayerCharacter->Player] + FP._0_50;
				return;
			}

			filter.BotCharacter->NextDecisionTime = f.Time + filter.BotCharacter->DecisionInterval;

			// We stop aiming after the use of special because real players can't shoot and use specials at the same time
			// So we don't allow bots to do it as well
			if (TryUseSpecials(f, ref filter))
			{
				StopAiming(f, ref filter);
			}

			// In case a bot has a gun and ammo but switched to a hammer - we switch back to a gun
			if (filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) &&
				f.TryGet<Stats>(filter.BotCharacter->MoveTarget, out var stats) && stats.CurrentAmmoPercent > FP._0)
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


			if (!FPMathHelpers.IsPositionInsideCircle(circleTargetCenter, circleTargetRadius, filter.Transform->Position.XZ) && circleTimeToShrink < filter.BotCharacter->TimeStartRunningFromCircle)
			{
				TryGoToSafeArea(f, ref filter, circleTargetCenter, circleTargetRadius);
				LogAction(ref filter, "go to safe area");

				return;
			}

			// Let it finish the path
			if (filter.NavMeshAgent->IsActive)
			{
				LogAction(ref filter, "wait for path to finish waipoint_count:" + filter.NavMeshAgent->WaypointCount);
				return;
			}

			if (TryGoForClosestCollectable(f, ref filter, circleCenter, circleRadius, circleIsShrinking))
			{
				LogAction(ref filter, "go to collectable");
				return;
			}

			if (TryStayCloseToTeammate(f, ref filter, circleCenter, circleRadius, circleIsShrinking))
			{
				LogAction(ref filter, "stay close to team mate");
				return;
			}

			if (WanderInsideCircle(f, ref filter, circleCenter, circleRadius))
			{
				LogAction(ref filter, "wander");
				return;
			}

			LogAction(ref filter, "no action");
		}


		public void GoToCenterOfCircle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter)
		{
			var newPosition = circleCenter;
			// We try to go into random position OR into circle center (it's good for a very small circle)
			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, newPosition.XOY))
			{
				filter.BotCharacter->MoveTarget = filter.Entity;
				filter.BotCharacter->NextDecisionTime = f.Time + FP._5;
			}
		}

		private void StopAiming(Frame f, ref BotCharacterFilter filter)
		{
			var speed = f.Get<Stats>(filter.Entity).Values[(int)StatType.Speed].StatValue;
			speed *= filter.BotCharacter->MovementSpeedMultiplier;

			var speedUpMutatorExists = f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
			speed = speedUpMutatorExists ? speed * speedUpMutatorConfig.Param1 : speed;

			// When we clear the target we also return speed to normal
			// because without a target bots don't shoot
			f.Unsafe.GetPointer<CharacterController3D>(filter.Entity)->MaxSpeed = speed;

			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);
			bb->Set(f, Constants.IsAimPressedKey, false);
		}

		private void ClearTarget(Frame f, ref BotCharacterFilter filter)
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

		// We loop through players to find a reference for alive teammate in case current is dead
		private void CheckOnTeammates(Frame f, ref BotCharacterFilter filter)
		{
			if (!QuantumHelpers.IsDestroyed(f, filter.BotCharacter->RandomTeammate))
			{
				return;
			}

			var randomTeammate = EntityRef.None;
			var team = f.Get<Targetable>(filter.Entity).Team;

			foreach (var candidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
			{
				if (candidate.Component->Team == team)
				{
					randomTeammate = candidate.Entity;
					break;
				}
			}

			// If no teammates are alive then we let a bot think that they are alone in their team to not look for teammates anymore
			if (randomTeammate == EntityRef.None)
			{
				filter.BotCharacter->TeamSize = 1;
			}

			filter.BotCharacter->RandomTeammate = randomTeammate;
		}

		// We loop through targetable entities trying to find if any is eligible to shoot at
		private void CheckEnemiesToShooAt(Frame f, ref BotCharacterFilter filter, ref QuantumWeaponConfig weaponConfig)
		{
			var target = EntityRef.None;

			// We do line/shapecasts for enemies in sight
			// If there is a target in Sight then store this Target into the blackboard variable
			// We check enemies one by one until we find a valid enemy in sight
			// Note: Bots against bots use the full weapon range
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

		// We check specific entity if a bot can hit it or not, to make a decision to aim or not to aim
		// Note that as a result we can get another entity that is being hit, for instance if it appears between the bot and a target that we are checking
		private bool TryToAimAtEnemy(Frame f, ref BotCharacterFilter filter, FPVector3 botPosition, int team,
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


			// Are bots inside eachother
			if (FPVector3.DistanceSquared(botPosition, targetPosition) < FP._0_20)
			{
				var random = FPVector3.Normalize(botPosition - targetPosition) * f.RNG->NextInclusive(FP._1, FP._3);
				var randomPosition = botPosition + random;
				if (QuantumHelpers.SetClosestTarget(f, filter.Entity, randomPosition))
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

		// We check specials and try to use them depending on their type if possible
		private bool TryUseSpecials(Frame f, ref BotCharacterFilter filter)
		{
			if (f.Time < filter.BotCharacter->NextAllowedSpecialUseTime)
			{
				return false;
			}
			if (!(f.RNG->Next() < filter.BotCharacter->ChanceToUseSpecial))
			{
				return false;
			}

			for (var i = 0; i <= 1; i++)
			{
				if (TryUseSpecial(f, filter.PlayerCharacter, i, filter.Entity, filter.BotCharacter->Target))
				{
					filter.BotCharacter->NextAllowedSpecialUseTime = f.Time + f.RNG->NextInclusive(filter.BotCharacter->SpecialCooldown);
					return true;
				}

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

		private bool TryGoForClosestCollectable(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking)
		{
			// Strategy is to pick up everything you can possible pick up
			// So we will look at closest pickups and discard things that we can't / don't need to pickup

			var sqrDistance = FP.MaxValue;
			var collectablePosition = FPVector3.Zero;
			var collectableEntity = EntityRef.None;
			var iterator = f.Unsafe.GetComponentBlockIterator<Collectable>();

			var botPosition = filter.Transform->Position;
			var stats = f.Get<Stats>(filter.Entity);
			var maxShields = stats.Values[(int)StatType.Shield].StatValue;
			var currentAmmo = stats.CurrentAmmoPercent;
			var maxHealth = stats.Values[(int)StatType.Health].StatValue;

			var needWeapon = filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) || currentAmmo < FP.SmallestNonZero;
			var needAmmo = currentAmmo < FP._0_99;
			var needShields = stats.CurrentShield < maxShields;
			var needHealth = stats.CurrentHealth < maxHealth;

			var teamMembers = TeamHelpers.GetTeamMembers(f, filter.Entity);
			var invalidTargets = f.ResolveHashSet(filter.BotCharacter->InvalidMoveTargets);
			foreach (var collectableCandidate in iterator)
			{
				if (invalidTargets.Contains(collectableCandidate.Entity))
				{
					continue;
				}

				if (collectableCandidate.Component->GameId.IsInGroup(GameIdGroup.Weapon) && !needWeapon)
				{
					continue;
				}

				// If team mate is collecting ignore it!
				var teamMemberCollecting =
					teamMembers.Any(member =>
					{
						if (collectableCandidate.Component->IsCollecting(member.Component->Player))
						{
							return true;
						}

						if (f.TryGet<BotCharacter>(member.Entity, out var otherBot))
						{
							if (otherBot.MoveTarget == collectableCandidate.Entity)
							{
								return true;
							}
						}

						return false;
					});

				if (teamMemberCollecting)
				{
					continue;
				}


				if (f.TryGet<Consumable>(collectableCandidate.Entity, out var consumable))
				{
					var usefulConsumable = consumable.ConsumableType switch
					{
						ConsumableType.Ammo   => needAmmo,
						ConsumableType.Shield => needShields,
						ConsumableType.Health => needHealth,
						_                     => true
					};

					if (!usefulConsumable)
					{
						continue;
					}
				}

				var positionCandidate = f.Get<Transform3D>(collectableCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter)
					&& newSqrDistance < sqrDistance
					&& IsInCircle(f, ref filter, circleCenter, circleRadius, circleIsShrinking, positionCandidate))
				{
					sqrDistance = newSqrDistance;
					collectablePosition = positionCandidate;
					collectableEntity = collectableCandidate.Entity;
				}
			}

			if (collectableEntity == EntityRef.None)
			{
				return false;
			}

			if (filter.NavMeshAgent->IsActive
				&& filter.BotCharacter->MoveTarget == collectableEntity)
			{
				return true;
			}

			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, collectablePosition))
			{
				//Log.Warn("Bot: " + filter.Entity.Index + "; moves to Collectable: " + collectableEntity + " at " + collectablePosition);

				filter.BotCharacter->MoveTarget = collectableEntity;

				return true;
			}

			return false;
		}

		private bool TryGoToSafeArea(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius)
		{
			// Not all game modes have a Shrinking Circle
			if (circleRadius < FP.SmallestNonZero)
			{
				return false;
			}

			var botPosition = filter.Transform->Position.XZ;

			var circleToVector = FPVector2.Normalize(botPosition - circleCenter);
			var range = circleRadius * (FP._0_75 + f.RNG->NextInclusive(FP._0_10 * FP.Minus_1, FP._0_10));
			var intersectionPoint = circleCenter + (circleToVector * range);
			var newPosition = intersectionPoint;

			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, newPosition.XOY))
			{
				filter.BotCharacter->NextDecisionTime = f.Time + FP._10;
				filter.BotCharacter->MoveTarget = filter.Entity;
				LogAction(ref filter, $"Going towards direction random radius center:{circleCenter} Random:{newPosition} bot:{filter.Transform->Position}");
				return true;
			}

			// We try to go into random position OR into circle center (it's good for a very small circle)
			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, circleCenter.XOY))
			{
				LogAction(ref filter, $"Going towards center: Center:{circleCenter} Random:{newPosition} bot:{filter.Transform->Position}");

				filter.BotCharacter->NextDecisionTime = f.Time + FP._10;
				filter.BotCharacter->MoveTarget = filter.Entity;
				return true;
			}

			return false;
		}

		private bool TryStayCloseToTeammate(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking)
		{
			if (filter.BotCharacter->TeamSize <= 1 || QuantumHelpers.IsDestroyed(f, filter.BotCharacter->RandomTeammate))
			{
				return false;
			}

			var teammatePosition = filter.BotCharacter->RandomTeammate.GetPosition(f);
			var botPosition = filter.Transform->Position;
			var vectorToTeammate = teammatePosition - botPosition;
			var maxDistanceSquared = filter.BotCharacter->MaxDistanceToTeammateSquared;

			if (vectorToTeammate.SqrMagnitude < maxDistanceSquared)
			{
				return false;
			}

			var destination = filter.Transform->Position + vectorToTeammate.Normalized * (vectorToTeammate.Magnitude / FP._2);
			var isGoing = IsInCircle(f, ref filter, circleCenter, circleRadius, circleIsShrinking, destination)
				&& QuantumHelpers.SetClosestTarget(f, filter.Entity, destination);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = filter.Entity;
			}

			return isGoing;
		}

		private bool IsInVisionRange(FP distanceSqr, ref BotCharacterFilter filter)
		{
			var visionRangeSqr = filter.BotCharacter->VisionRangeSqr;

			return visionRangeSqr < FP._0 || distanceSqr <= visionRangeSqr;
		}

		private bool IsInCircle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking, FPVector3 positionToCheck)
		{
			// If circle doesn't exist then we always return true
			if (circleRadius < FP.SmallestNonZero)
			{
				return true;
			}

			var distanceSqr = (positionToCheck.XZ - circleCenter).SqrMagnitude;

			// If circle is shrinking then it's risky to get to consumables on the edge so we don't do it
			if (circleIsShrinking)
			{
				return distanceSqr <= (circleRadius * circleRadius) * (FP._0_20 + FP._0_10);
			}

			return distanceSqr <= (circleRadius * circleRadius) * (FP._0_75);
		}


		/// <summary>
		/// It goes around a invisible circle to wonder in the borders
		/// </summary>
		public bool WanderInsideCircle(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius)
		{
			if (!TryToWanderToDirection(f, ref filter, circleCenter, circleRadius, filter.BotCharacter->WanderDirection))
			{
				// If fails to move change direction and try again
				filter.BotCharacter->WanderDirection = !filter.BotCharacter->WanderDirection;
				return TryToWanderToDirection(f, ref filter, circleCenter, circleRadius, filter.BotCharacter->WanderDirection);
			}

			return true;
		}


		private bool TryToWanderToDirection(Frame f, ref BotCharacterFilter filter, FPVector2 circleCenter, FP circleRadius, bool clockwise)
		{
			var position = filter.Transform->Position.XZ;

			// Player angle in relation to the center of the circle
			var distanceToCenter = FPVector2.Distance(position, circleCenter);
			var direction = position - circleCenter;
			var currentAngle = FPMath.Atan2(direction.Y, direction.X);


			// Let the player wander between 0 and 45º around the circle 
			// a bot has a fixed angle increase so doesn't keep going back to the same position, always going around the circle
			var randomizedAngle = currentAngle + f.RNG->NextInclusive(FP._0_20, FP._0_75) * (clockwise ? FP._1 : FP.Minus_1);

			// Also varying radius to be more natural, but a little bit per course adjust
			var radiusVariation = (circleRadius * FP._0_10) * f.RNG->NextInclusive(FP.Minus_1 * FP._1_50, FP._1_50);
			var playerTargetRadius = FPMath.Abs(distanceToCenter + radiusVariation);
			// and to be safe do not let the player go too close to the border
			var randomizedRadius = FPMath.Min(circleRadius * (FP._0_75), playerTargetRadius);


			var x = circleCenter.X + randomizedRadius * FPMath.Cos(randomizedAngle);
			var y = circleCenter.Y + randomizedRadius * FPMath.Sin(randomizedAngle);
			LogAction(ref filter, @$"From angle {FP.Rad2Deg * currentAngle} target angle: {FP.Rad2Deg * randomizedAngle}
From radius {distanceToCenter} to radius {randomizedRadius}
From position {position} to position ({x},{y})
");

			return QuantumHelpers.SetClosestTarget(f, filter.Entity, new FPVector3(x, FP._0, y));
		}

		public void OnNavMeshWaypointReached(Frame f, EntityRef entity, FPVector3 waypoint, Navigation.WaypointFlag waypointFlags, ref bool resetAgent)
		{
			LogAction(entity, $"Navmesh path ({waypointFlags.ToString()}) reached");

			// if the navigation finished
			if ((waypointFlags & Navigation.WaypointFlag.LinkEnd) == 0) return;

			if (!f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot)) return;

			// If the target is the bot it means is moving to another position in the map
			if (bot->MoveTarget == entity)
			{
				bot->MoveTarget = EntityRef.None;
				// let the bot do a quick decision after this
				bot->NextDecisionTime = f.Time + FP._0_01;
			}
		}

		public void OnNavMeshSearchFailed(Frame f, EntityRef entity, ref bool resetAgent)
		{
			LogAction(entity, "pathfinding failed");

			if (f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot))
			{
				if (bot->MoveTarget != EntityRef.None && bot->MoveTarget != entity)
				{
					var invalid = f.ResolveHashSet(bot->InvalidMoveTargets);
					invalid.Add(bot->MoveTarget);
				}

				bot->MoveTarget = EntityRef.None;
				// If the target is the bot it means is moving to another position in the map
				// let the bot do a quick decision after this
				bot->NextDecisionTime = f.Time + FP._0_01;
			}
		}

		[Conditional("BOT_DEBUG")]
		private void LogAction(ref BotCharacterFilter filter, string action)
		{
			Log.Warn("Bot " + filter.BotCharacter->BotNameIndex + " " + filter.Entity + " took decision " + action);
		}

		[Conditional("BOT_DEBUG")]
		private void LogAction(EntityRef entity, string action)
		{
			Log.Warn("Bot " + entity + " took decision " + action);
		}

		public void OnRemoved(Frame f, EntityRef entity, BotCharacter* component)
		{
			f.FreeHashSet(component->InvalidMoveTargets);
			component->InvalidMoveTargets = default;
		}
	}
}