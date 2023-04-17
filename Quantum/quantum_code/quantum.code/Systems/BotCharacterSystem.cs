using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="BotCharacter"/>
	/// </summary>
	public unsafe class BotCharacterSystem : SystemMainThreadFilter<BotCharacterSystem.BotCharacterFilter>,
											 ISignalAllPlayersSpawned
	{
		public struct BotCharacterFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public BotCharacter* BotCharacter;
			public PlayerCharacter* PlayerCharacter;
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
			InitializeBots(f, averagePlayerTrophies);
		}

		private void InitializeBots(Frame f, uint baseTrophiesAmount)
		{
			if (!f.Context.GameModeConfig.AllowBots || f.ComponentCount<BotCharacter>() > 0)
			{
				return;
			}

			var playerLimit = f.PlayerCount;
			var botIds = new List<PlayerRef>();

			var maxPlayers = Math.Min(f.Context.MapConfig.MaxPlayers, f.Context.GameModeConfig.MaxPlayers);
			if (playerLimit == 1 && maxPlayers > 1) // offline game with bots
			{
				playerLimit = (int)maxPlayers;
			}
			
			for (var i = 0; i < playerLimit; i++)
			{
				if (i >= f.PlayerCount || (f.GetPlayerInputFlags(i) & DeterministicInputFlags.PlayerNotPresent) ==
				    DeterministicInputFlags.PlayerNotPresent)
				{
					botIds.Add(i);
				}
			}

			if (botIds.Count != playerLimit)
			{
				AddBots(f, botIds, baseTrophiesAmount);
			}
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

			// If bot is not grounded the we explicitly call Move to apply gravity
			// It's because even with Zero velocity any movement of CharacterController,
			// even the internal gravitational one, is being processed ONLY when we call the "Move" method
			if (!kcc->Grounded)
			{
				kcc->Velocity = FPVector3.Down * FP._8;
				kcc->Move(f, filter.Entity, FPVector3.Zero);

				// TODO Nik: Make a specific branching decision in case we skydive in Battle Royale
				// instead of just Move to zero direction we need to choose a target to move to, based on bot BotBehaviourType,
				// then store this target in blackboard (to not search again) and keep moving towards it

				return;
			}

			// If a bot has a valid target then we correct the bot's speed according
			// to the weapon they carry and turn the bot towards the target
			// otherwise we return speed to normal and let automatic navigation turn the bot
			var target = filter.BotCharacter->Target;
			var speed = f.Get<Stats>(filter.Entity).Values[(int) StatType.Speed].StatValue;
			speed *= filter.BotCharacter->MovementSpeedMultiplier;
			var weaponConfig = f.WeaponConfigs.GetConfig(filter.PlayerCharacter->CurrentWeapon.GameId);

			// We need to check also for AlivePlayerCharacter because with respawns we don't destroy Player Entities
			if (QuantumHelpers.IsDestroyed(f, target) || !f.Has<AlivePlayerCharacter>(target))
			{
				ClearTarget(f, ref filter);
			}
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
					speed *= weaponConfig.AimingMovementSpeed;

					kcc->MaxSpeed = speedUpMutatorExists ? speed * speedUpMutatorConfig.Param1 : speed;

					filter.BotCharacter->Target = targetHit;
					QuantumHelpers.LookAt2d(f, filter.Entity, targetHit);
					bb->Set(f, Constants.IsAimPressedKey, true);
					target = targetHit;
				}
				else
				{
					ClearTarget(f, ref filter);
				}
			}

			// Bots look for others to shoot at not on every frame
			if (f.Time > filter.BotCharacter->NextLookForTargetsToShootAtTime)
			{
				// Check if target exists otherwise look for new enemies
				if (filter.BotCharacter->Target != EntityRef.None)
				{
					// Bots have a ChanceToAbandonTarget to stop shooting/tracking the target to allow more room for players to escape
					// Versus other bots this chance is 4 times lower
					if (f.RNG->Next() < (f.Has<BotCharacter>(filter.BotCharacter->Target)
						    ? filter.BotCharacter->ChanceToAbandonTarget * FP._0_25
						    : filter.BotCharacter->ChanceToAbandonTarget))
					{
						ClearTarget(f, ref filter);
					}
					else
					{
						// Checking how close is the target and stop the movement if the target is closer
						// than allowed by closefight intolerance
						var weaponTargetRange =
							FPMath.Min(f.Get<Stats>(filter.Entity).GetStatData(StatType.AttackRange).StatValue,
								filter.BotCharacter->MaxAimingRange);
						var minDistanceToTarget =
							FPMath.Max(FP._1_50, weaponTargetRange * filter.BotCharacter->CloseFightIntolerance);
						var sqrDistanceToTarget = (f.Get<Transform3D>(target).Position - filter.Transform->Position)
							.SqrMagnitude;

						// If target is too far then we stop attacking
						if (sqrDistanceToTarget > weaponTargetRange * weaponTargetRange)
						{
							ClearTarget(f, ref filter);
						}
						// If the bot was moving towards enemy or not moving anywhere then we do distance checks to not get too close
						else if ((filter.BotCharacter->MoveTarget == target ||
									 filter.BotCharacter->MoveTarget == EntityRef.None)
								 && sqrDistanceToTarget < minDistanceToTarget * minDistanceToTarget)
						{
							filter.BotCharacter->MoveTarget = EntityRef.None;
							filter.NavMeshAgent->Stop(f, filter.Entity, true);
						}
					}
				}
				else
				{
					CheckEnemiesToShooAt(f, ref filter, ref weaponConfig);
				}

				filter.BotCharacter->NextLookForTargetsToShootAtTime =
					f.Time + filter.BotCharacter->LookForTargetsToShootAtInterval;
			}

			// Check move target in case it disappeared or a bot collected it and needs to move on
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
			    QuantumHelpers.IsDestroyed(f, filter.BotCharacter->MoveTarget))
			{
				filter.BotCharacter->MoveTarget = EntityRef.None;
				filter.NavMeshAgent->Stop(f, filter.Entity, true);
			}

			var botStuckWandering = filter.NavMeshAgent->IsActive == false &&
				filter.BotCharacter->MoveTarget == filter.Entity;

			if (filter.BotCharacter->TeamSize > 1)
			{
				CheckOnTeammates(f, ref filter);
			}

			// Do not do any decision making if the time has not come, unless a bot stucked wandering or collected a target or has no one to shoot at
			if (!botStuckWandering
			    && (filter.BotCharacter->MoveTarget != EntityRef.None || filter.BotCharacter->Target != EntityRef.None)
			    && f.Time < filter.BotCharacter->NextDecisionTime)
			{
				return;
			}

			// Check that bot isn't stuck in place. If it does then force repath
			// If this solution won't prevent bots from standing in one place doing nothing then we should consider
			// doing check "filter.NavMeshAgent->IsOnLink(f)" instead of "filter.NavMeshAgent->IsActive"
			// because sometimes "filter.NavMeshAgent->IsActive" is TRUE even though the actor doesn't visually move,
			// hence why we are Forcing Repath;
			// Another reason why a bot isn't going anywhere can be because the point they want to go to is
			// unreachable due to how navmesh were baked or consumable placed
			// if (FPVector3.DistanceSquared(filter.BotCharacter->StuckDetectionPosition, 
			//                               filter.Transform->Position) < Constants.BOT_STUCK_DETECTION_DISTANCE)
			// {
			// 	filter.NavMeshAgent->ForceRepath(f);
			// }

			filter.BotCharacter->NextDecisionTime = f.Time + filter.BotCharacter->DecisionInterval;
			filter.BotCharacter->StuckDetectionPosition = filter.Transform->Position;

			// We call ClearTarget after the use of special because real players can't shoot and use specials at the same time
			// So we don't allow bots to do it as well
			if (TryUseSpecials(f, ref filter))
			{
				ClearTarget(f, ref filter);
			}

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

			switch (filter.BotCharacter->BehaviourType)
			{
				case BotBehaviourType.Static:
					break;
				case BotBehaviourType.Dumb:
					var dumb = TryAvoidShrinkingCircle(f, ref filter)
						|| TryGoForGear(f, ref filter)
						|| TryGoForHealth(f, ref filter)
						|| TryGoForShield(f, ref filter)
						|| TryGoForAmmo(f, ref filter)
						|| TryGoForWeapons(f, ref filter)
						|| TryGoForCrates(f, ref filter)
						|| TryGoForRage(f, ref filter)
						|| TryGoForEnemies(f, ref filter, weaponConfig)
						|| Wander(f, ref filter);
					break;
				case BotBehaviourType.Cautious:
					var cautious = TryAvoidShrinkingCircle(f, ref filter)
						|| TryStayCloseToTeammate(f, ref filter)
						|| TryGoForGear(f, ref filter)
						|| TryGoForHealth(f, ref filter)
						|| TryGoForShield(f, ref filter)
						|| TryGoForAmmo(f, ref filter)
						|| TryGoForWeapons(f, ref filter)
						|| TryGoForCrates(f, ref filter)
						|| TryGoForRage(f, ref filter)
						|| TryGoForEnemies(f, ref filter, weaponConfig)
						|| Wander(f, ref filter);
					break;
				case BotBehaviourType.Aggressive:
					var aggressive = TryAvoidShrinkingCircle(f, ref filter)
						|| TryStayCloseToTeammate(f, ref filter)
						|| TryGoForGear(f, ref filter)
						|| TryGoForRage(f, ref filter)
						|| TryGoForWeapons(f, ref filter)
						|| TryGoForAmmo(f, ref filter)
						|| TryGoForCrates(f, ref filter)
						|| TryGoForEnemies(f, ref filter, weaponConfig)
						|| TryGoForShield(f, ref filter)
						|| TryGoForHealth(f, ref filter)
						|| Wander(f, ref filter);
					break;
				case BotBehaviourType.Balanced:
					var balanced = TryAvoidShrinkingCircle(f, ref filter)
						|| TryStayCloseToTeammate(f, ref filter)
						|| TryGoForGear(f, ref filter)
						|| TryGoForAmmo(f, ref filter)
						|| TryGoForHealth(f, ref filter)
						|| TryGoForWeapons(f, ref filter)
						|| TryGoForShield(f, ref filter)
						|| TryGoForCrates(f, ref filter)
						|| TryGoForEnemies(f, ref filter, weaponConfig)
						|| TryGoForRage(f, ref filter)
						|| Wander(f, ref filter);
					break;
			}
		}

		private List<QuantumBotConfig> GetBotConfigsList(Frame f, int botsDifficulty)
		{
			var list = new List<QuantumBotConfig>();
			var configs = f.BotConfigs.QuantumConfigs;
			var difficultyLevel = botsDifficulty;
			var botGamemodeKey = f.Context.GameModeConfig.UseBotsFromGamemode;
			if (botGamemodeKey == null || botGamemodeKey.Trim().Length == 0)
			{
				botGamemodeKey = f.Context.GameModeConfig.Id;
			}

			foreach (var botConfig in configs)
			{
				if (botConfig.Difficulty == difficultyLevel && botConfig.GameMode == botGamemodeKey)
				{
					list.Add(botConfig);
				}
			}

			return list;
		}

		private void ClearTarget(Frame f, ref BotCharacterFilter filter)
		{
			var speed = f.Get<Stats>(filter.Entity).Values[(int) StatType.Speed].StatValue;
			speed *= filter.BotCharacter->MovementSpeedMultiplier;

			// If the bot was moving towards this enemy then we clear move target and force a bot to make a decision
			if (filter.BotCharacter->MoveTarget == filter.BotCharacter->Target)
			{
				filter.BotCharacter->MoveTarget = EntityRef.None;
				filter.NavMeshAgent->Stop(f, filter.Entity, true);
			}

			filter.BotCharacter->Target = EntityRef.None;

			var speedUpMutatorExists = f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
			speed = speedUpMutatorExists ? speed * speedUpMutatorConfig.Param1 : speed;

			// When we clear the target we also return speed to normal
			// because without a target bots don't shoot
			f.Unsafe.GetPointer<CharacterController3D>(filter.Entity)->MaxSpeed = speed;

			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);
			bb->Set(f, Constants.IsAimPressedKey, false);
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
			var weaponTargetRangeAgainstBots = f.Get<Stats>(filter.Entity).GetStatData(StatType.AttackRange).StatValue;
			var weaponTargetRange = FPMath.Min(weaponTargetRangeAgainstBots, filter.BotCharacter->MaxAimingRange);
			var botPosition = filter.Transform->Position;
			var team = f.Get<Targetable>(filter.Entity).Team;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);

			botPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

			foreach (var targetCandidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
			{
				if (TryToAimAtEnemy(f, ref filter, botPosition, team,
					    f.Has<BotCharacter>(targetCandidate.Entity) ? weaponTargetRangeAgainstBots : weaponTargetRange,
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

		// We check specials and try to use them depending on their type if possible
		private bool TryUseSpecials(Frame f, ref BotCharacterFilter filter)
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

		private bool TryAvoidShrinkingCircle(Frame f, ref BotCharacterFilter filter)
		{
			// Not all game modes have a Shrinking Circle
			if (!f.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				return false;
			}

			if (filter.NavMeshAgent->IsActive && filter.BotCharacter->MoveTarget == filter.Entity
			    && f.Time < filter.BotCharacter->CurrentEvasionStepEndTime)
			{
				return true;
			}

			var sqrDistanceFromSafeAreaCenter =
				FPVector2.DistanceSquared(filter.Transform->Position.XZ, circle.TargetCircleCenter);
			var sqrRadiusOfShrinkingCircle = circle.CurrentRadius * circle.CurrentRadius;

			// If a bot is inside the circle and the circle is not shrinking then a bot doesn't try to avoid it
			if (f.Time < circle.ShrinkingStartTime && sqrDistanceFromSafeAreaCenter < sqrRadiusOfShrinkingCircle)
			{
				return false;
			}

			var sqrSafeAreaRadius = circle.TargetRadius * circle.TargetRadius;
			var safeCircleCenter = circle.TargetCircleCenter.XOY;
			safeCircleCenter.Y = filter.Transform->Position.Y;

			// If sqrDistanceFromSafeAreaCenter / sqrSafeAreaRadius > 1 then the bot is outside the safe area
			// If sqrDistanceFromSafeAreaCenter / sqrSafeAreaRadius < 1 then the bot is safe, inside safe area

			var isGoing = sqrDistanceFromSafeAreaCenter / sqrSafeAreaRadius >
				filter.BotCharacter->ShrinkingCircleRiskTolerance;

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, safeCircleCenter,
				filter.BotCharacter->WanderRadius);

			if (isGoing)
			{
				// We are setting "self" as a target when there's no specific entity a bot is moving towards
				filter.BotCharacter->MoveTarget = filter.Entity;
				filter.BotCharacter->CurrentEvasionStepEndTime = f.Time + filter.BotCharacter->DecisionInterval;
			}

			return isGoing;
		}

		private bool TryStayCloseToTeammate(Frame f, ref BotCharacterFilter filter)
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
			var isGoing = QuantumHelpers.SetClosestTarget(f, filter.Entity, destination);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = filter.Entity;
			}

			return isGoing;
		}

		private bool TryGoForRage(Frame f, ref BotCharacterFilter filter)
		{
			if (!(f.RNG->Next() < filter.BotCharacter->ChanceToSeekRage))
			{
				return false;
			}

			var isGoing = TryGetClosestConsumable(f, ref filter, ConsumableType.Rage, out var rageConsumablePosition,
				out var rageConsumableEntity);

			if (isGoing && filter.NavMeshAgent->IsActive && filter.BotCharacter->MoveTarget == rageConsumableEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, rageConsumablePosition);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = rageConsumableEntity;
			}

			return isGoing;
		}

		private bool TryGoForShield(Frame f, ref BotCharacterFilter filter)
		{
			var armourConsumablePosition = FPVector3.Zero;
			var armourConsumableEntity = EntityRef.None;

			var stats = f.Get<Stats>(filter.Entity);
			var maxArmour = stats.Values[(int) StatType.Shield].StatValue;
			var ratioArmour = stats.CurrentShield / maxArmour;
			var lowArmourSensitivity = filter.BotCharacter->LowArmourSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioArmour) * lowArmourSensitivity);

			// If we don't go for shield consumable then try to go for capacity if needed
			if (!isGoing)
			{
				var maxCapacity = stats.Values[(int) StatType.Shield].BaseValue;
				isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - maxArmour / maxCapacity) * lowArmourSensitivity);

				isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.ShieldCapacity,
					out armourConsumablePosition, out armourConsumableEntity);
			}
			else
			{
				isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.Shield,
					out armourConsumablePosition, out armourConsumableEntity);
			}

			if (isGoing && filter.NavMeshAgent->IsActive && filter.BotCharacter->MoveTarget == armourConsumableEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, armourConsumablePosition);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = armourConsumableEntity;
			}

			return isGoing;
		}

		private bool TryGoForHealth(Frame f, ref BotCharacterFilter filter)
		{
			var healthConsumablePosition = FPVector3.Zero;
			var healthConsumableEntity = EntityRef.None;
			var stats = f.Get<Stats>(filter.Entity);
			var maxHealth = stats.Values[(int) StatType.Health].StatValue;
			var ratioHealth = stats.CurrentHealth / maxHealth;
			var lowHealthSensitivity = filter.BotCharacter->LowHealthSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioHealth) * lowHealthSensitivity);

			isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.Health,
				out healthConsumablePosition, out healthConsumableEntity);

			if (isGoing && filter.NavMeshAgent->IsActive && filter.BotCharacter->MoveTarget == healthConsumableEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, healthConsumablePosition);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = healthConsumableEntity;
			}

			return isGoing;
		}

		private bool TryGoForAmmo(Frame f, ref BotCharacterFilter filter)
		{
			var ammoConsumablePosition = FPVector3.Zero;
			var ammoConsumableEntity = EntityRef.None;

			// If weapon has Unlimited ammo then don't go for more ammo UNLESS a bot also has a gun in another slot 
			if (filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity)
			    && !filter.PlayerCharacter->WeaponSlots[1].Weapon.IsValid()
			    && !filter.PlayerCharacter->WeaponSlots[2].Weapon.IsValid())
			{
				return false;
			}

			var ratioAmmo = filter.PlayerCharacter->GetAmmoAmountFilled(f, filter.Entity);
			var lowAmmoSensitivity = filter.BotCharacter->LowAmmoSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioAmmo) * lowAmmoSensitivity);

			isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.Ammo,
				out ammoConsumablePosition, out ammoConsumableEntity);

			if (isGoing && filter.NavMeshAgent->IsActive && filter.BotCharacter->MoveTarget == ammoConsumableEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, ammoConsumablePosition);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = ammoConsumableEntity;
			}

			return isGoing;
		}

		private bool TryGoForCrates(Frame f, ref BotCharacterFilter filter)
		{
			if (!f.Context.GameModeConfig.BotSearchForCrates)
			{
				return false;
			}

			var chestPosition = FPVector3.Zero;
			var chestEntity = EntityRef.None;
			var stats = f.Get<Stats>(filter.Entity);
			var chance = filter.BotCharacter->ChanceToSeekChests;

			// Chance to seek a crate is affected by other things, like current ammo, health etc. because
			// those things a bot can get from a crate
			var ammoChanceModifier = (FP._1 - filter.PlayerCharacter->GetAmmoAmountFilled(f, filter.Entity)) *
				filter.BotCharacter->LowAmmoSensitivity;
			var healthChanceModifier = (FP._1 - stats.CurrentHealth / stats.Values[(int) StatType.Health].StatValue) *
				filter.BotCharacter->LowHealthSensitivity;
			var shieldChanceModifier = (FP._1 - stats.CurrentShield / stats.Values[(int) StatType.Shield].StatValue) *
				filter.BotCharacter->LowArmourSensitivity;
			var weaponChanceModifier = FP._0;

			if (!filter.PlayerCharacter->WeaponSlots[1].Weapon.IsValid() &&
			    !filter.PlayerCharacter->WeaponSlots[2].Weapon.IsValid())
			{
				weaponChanceModifier = filter.BotCharacter->ChanceToSeekWeapons;
			}

			chance += (ammoChanceModifier + healthChanceModifier + shieldChanceModifier + weaponChanceModifier) /
				(FP._100 * FP._4);

			var isGoing = f.RNG->Next() < chance;

			isGoing = isGoing && TryGetClosestChest(f, ref filter, out chestPosition, out chestEntity);

			if (isGoing && filter.NavMeshAgent->IsActive && filter.BotCharacter->MoveTarget == chestEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, chestPosition);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = chestEntity;
			}

			return isGoing;
		}

		private bool TryGoForGear(Frame f, ref BotCharacterFilter filter)
		{
			var gearPickupPosition = FPVector3.Zero;
			var gearPickupEntity = EntityRef.None;

			var isGoing = filter.BotCharacter->LoadoutGearNumber > 0;

			isGoing = isGoing && TryGetClosestGear(f, ref filter, out gearPickupPosition, out gearPickupEntity);

			if (isGoing && filter.NavMeshAgent->IsActive && filter.BotCharacter->MoveTarget == gearPickupEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, gearPickupPosition);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = gearPickupEntity;
			}

			return isGoing;
		}

		private bool TryGoForWeapons(Frame f, ref BotCharacterFilter filter)
		{
			var weaponPickupPosition = FPVector3.Zero;
			var weaponPickupEntity = EntityRef.None;

			var isGoing = f.Context.GameModeConfig.BotWeaponSearchStrategy switch
			{
				BotWeaponSearchStrategy.None => false,
				BotWeaponSearchStrategy.FindOne =>
					!filter.PlayerCharacter->WeaponSlots[1].Weapon.IsValid() &&
					!filter.PlayerCharacter->WeaponSlots[2].Weapon.IsValid() &&
					f.RNG->Next() < filter.BotCharacter->ChanceToSeekWeapons,
				BotWeaponSearchStrategy.FindOneOrNoAmmoOrRandomChance =>
					filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) ||
					filter.PlayerCharacter->GetAmmoAmountFilled(f, filter.Entity) < FP.SmallestNonZero ||
					f.RNG->Next() < filter.BotCharacter->ChanceToSeekWeapons,
				_ => throw new ArgumentOutOfRangeException()
			};

			isGoing = isGoing && TryGetClosestWeapon(f, ref filter, out weaponPickupPosition, out weaponPickupEntity);

			if (isGoing && filter.NavMeshAgent->IsActive && filter.BotCharacter->MoveTarget == weaponPickupEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, weaponPickupPosition);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = weaponPickupEntity;
			}

			return isGoing;
		}

		private bool TryGoForEnemies(Frame f, ref BotCharacterFilter filter, QuantumWeaponConfig weaponConfig)
		{
			var isGoing = f.RNG->Next() < filter.BotCharacter->ChanceToSeekEnemies;

			if (!isGoing || filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity))
			{
				return false;
			}

			var enemyPosition = FPVector3.Zero;
			var iterator = f.Unsafe.GetComponentBlockIterator<Targetable>();
			var sqrDistance = FP.MaxValue;
			var team = f.Get<Targetable>(filter.Entity).Team;
			var botPosition = filter.Transform->Position;
			var enemyEntity = EntityRef.None;
			var hasShrinkingCircle = f.TryGetSingleton<ShrinkingCircle>(out var circle);

			foreach (var targetCandidate in iterator)
			{
				if (!QuantumHelpers.IsAttackable(f, targetCandidate.Entity, team))
				{
					continue;
				}

				var positionCandidate = f.Get<Transform3D>(targetCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter) && newSqrDistance < sqrDistance &&
				    (!hasShrinkingCircle || IsInCircle(ref filter, circle, positionCandidate)))
				{
					sqrDistance = newSqrDistance;
					enemyPosition = positionCandidate;
					enemyEntity = targetCandidate.Entity;
				}
			}

			if (enemyPosition == FPVector3.Zero)
			{
				return false;
			}

			var weaponTargetRange = FPMath.Min(f.Get<Stats>(filter.Entity).GetStatData(StatType.AttackRange).StatValue,
				filter.BotCharacter->MaxAimingRange);
			// Do not go closer than 1.5 meters to target
			var offsetDistance = FPMath.Max(FP._1_50, weaponTargetRange * filter.BotCharacter->CloseFightIntolerance);

			// If we are closer than offset distance already then we don't move towards this target any closer
			if (sqrDistance < offsetDistance * offsetDistance)
			{
				return false;
			}

			var reverseDirection = (enemyPosition - botPosition).Normalized;
			var offsetPosition = enemyPosition + reverseDirection * offsetDistance;

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, offsetPosition);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = enemyEntity;
			}

			return isGoing;
		}

		private bool Wander(Frame f, ref BotCharacterFilter filter)
		{
			filter.BotCharacter->MoveTarget = EntityRef.None;

			var hasShrinkingCircle = f.TryGetSingleton<ShrinkingCircle>(out var circle);
			var wanderRadius = filter.BotCharacter->WanderRadius;
			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity);
			var navMesh = f.NavMesh;

			if (hasShrinkingCircle)
			{
				wanderRadius = FPMath.Clamp(wanderRadius, FP._1, circle.TargetRadius / FP._2);
			}

			// We make several attempts to find a random position to wander to
			// to minimize a chance of a situation where a bot stands in place doing nothing
			for (int i = 1; i < 5; i++)
			{
				if (navMesh.FindRandomPointOnNavmesh(filter.Transform->Position, wanderRadius, f.RNG,
					    agent->RegionMask, out var closestPosition)
				    && (!hasShrinkingCircle || IsInCircle(ref filter, circle, closestPosition)))
				{
					agent->SetTarget(f, closestPosition, navMesh);

					// We are setting "self" as a target when there's no specific entity a bot is moving towards
					filter.BotCharacter->MoveTarget = filter.Entity;

					return true;
				}
			}

			return false;
		}

		// Method to get consumable without a specific "consumablePowerAmount"
		private bool TryGetClosestConsumable(Frame f, ref BotCharacterFilter filter, ConsumableType consumableType,
											 out FPVector3 consumablePosition, out EntityRef consumableEntity)
		{
			return TryGetClosestConsumable(f, ref filter, consumableType, -1, out consumablePosition,
				out consumableEntity);
		}

		private bool TryGetClosestConsumable(Frame f, ref BotCharacterFilter filter, ConsumableType consumableType,
											 int consumablePowerAmount, out FPVector3 consumablePosition,
											 out EntityRef consumableEntity)
		{
			var botPosition = filter.Transform->Position;
			var iterator = f.Unsafe.GetComponentBlockIterator<Consumable>();
			var sqrDistance = FP.MaxValue;
			var hasShrinkingCircle = f.TryGetSingleton<ShrinkingCircle>(out var circle);

			consumablePosition = FPVector3.Zero;
			consumableEntity = EntityRef.None;

			foreach (var consumableCandidate in iterator)
			{
				if (consumableCandidate.Component->ConsumableType != consumableType ||
				    consumablePowerAmount != -1 && consumablePowerAmount != consumableCandidate.Component->Amount)
				{
					continue;
				}

				var positionCandidate = f.Get<Transform3D>(consumableCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter)
				    && newSqrDistance < sqrDistance
				    && (!hasShrinkingCircle || IsInCircle(ref filter, circle, positionCandidate)))
				{
					sqrDistance = newSqrDistance;
					consumablePosition = positionCandidate;
					consumableEntity = consumableCandidate.Entity;
				}
			}

			return consumablePosition != FPVector3.Zero;
		}

		private bool TryGetClosestGear(Frame f, ref BotCharacterFilter filter, out FPVector3 gearPickupPosition,
									   out EntityRef gearPickupEntity)
		{
			var botPosition = filter.Transform->Position;
			var iterator = f.Unsafe.GetComponentBlockIterator<EquipmentCollectable>();
			var sqrDistance = FP.MaxValue;
			var hasShrinkingCircle = f.TryGetSingleton<ShrinkingCircle>(out var circle);
			gearPickupPosition = FPVector3.Zero;
			gearPickupEntity = EntityRef.None;

			foreach (var gearCandidate in iterator)
			{
				if (!f.Get<Collectable>(gearCandidate.Entity).GameId.IsInGroup(GameIdGroup.Gear))
				{
					continue;
				}

				var positionCandidate = f.Get<Transform3D>(gearCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter)
				    && newSqrDistance < sqrDistance
				    && (!hasShrinkingCircle || IsInCircle(ref filter, circle, positionCandidate)))
				{
					sqrDistance = newSqrDistance;
					gearPickupPosition = positionCandidate;
					gearPickupEntity = gearCandidate.Entity;
				}
			}

			return gearPickupPosition != FPVector3.Zero;
		}

		private bool TryGetClosestWeapon(Frame f, ref BotCharacterFilter filter, out FPVector3 weaponPickupPosition,
										 out EntityRef weaponPickupEntity)
		{
			var botPosition = filter.Transform->Position;
			var iterator = f.Unsafe.GetComponentBlockIterator<EquipmentCollectable>();
			var sqrDistance = FP.MaxValue;
			var hasShrinkingCircle = f.TryGetSingleton<ShrinkingCircle>(out var circle);
			var stats = f.Unsafe.GetPointer<Stats>(filter.Entity);
			var totalAmmo = stats->CurrentAmmo;
			var maxAmmo = stats->GetStatData(StatType.AmmoCapacity).StatValue;
			weaponPickupPosition = FPVector3.Zero;
			weaponPickupEntity = EntityRef.None;

			foreach (var weaponCandidate in iterator)
			{
				var weaponCandidateId = f.Get<Collectable>(weaponCandidate.Entity).GameId;

				// Do not pick up the same weapon unless has less than 50% ammo
				if (filter.PlayerCharacter->CurrentWeapon.GameId == weaponCandidateId && totalAmmo > maxAmmo * FP._0_50)
				{
					continue;
				}

				var positionCandidate = f.Get<Transform3D>(weaponCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter)
				    && newSqrDistance < sqrDistance
				    && (!hasShrinkingCircle || IsInCircle(ref filter, circle, positionCandidate)))
				{
					sqrDistance = newSqrDistance;
					weaponPickupPosition = positionCandidate;
					weaponPickupEntity = weaponCandidate.Entity;
				}
			}

			return weaponPickupPosition != FPVector3.Zero;
		}

		private bool TryGetClosestChest(Frame f, ref BotCharacterFilter filter, out FPVector3 chestPosition,
										out EntityRef chestEntity)
		{
			var botPosition = filter.Transform->Position;
			var iterator = f.Unsafe.GetComponentBlockIterator<Chest>();
			var sqrDistance = FP.MaxValue;
			var hasShrinkingCircle = f.TryGetSingleton<ShrinkingCircle>(out var circle);
			chestPosition = FPVector3.Zero;
			chestEntity = EntityRef.None;

			foreach (var chestCandidate in iterator)
			{
				var positionCandidate = f.Get<Transform3D>(chestCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter)
				    && newSqrDistance < sqrDistance
				    && (!hasShrinkingCircle || IsInCircle(ref filter, circle, positionCandidate)))
				{
					sqrDistance = newSqrDistance;
					chestPosition = positionCandidate;
					chestEntity = chestCandidate.Entity;
				}
			}

			return chestPosition != FPVector3.Zero;
		}

		private bool IsInVisionRange(FP distanceSqr, ref BotCharacterFilter filter)
		{
			var visionRangeSqr = filter.BotCharacter->VisionRangeSqr;

			return visionRangeSqr < FP._0 || distanceSqr <= visionRangeSqr;
		}

		private bool IsInCircle(ref BotCharacterFilter filter, ShrinkingCircle circle, FPVector3 positionToCheck)
		{
			var distanceSqr = FPVector2.DistanceSquared(positionToCheck.XZ, circle.CurrentCircleCenter);

			return distanceSqr <= circle.CurrentRadius * circle.CurrentRadius;
		}

		private void AddBots(Frame f, List<PlayerRef> botIds, uint baseTrophiesAmount)
		{
			var teamSize = f.Context.GameModeConfig.Teams ? (int) f.Context.GameModeConfig.MaxPlayersInTeam : 1;
			var playerSpawners = GetFreeSpawnPoints(f);
			var botsNameCount = f.GameConfig.BotsNameCount;
			var botNamesIndices = new List<int>(botsNameCount);
			var deathMakers = GameIdGroup.DeathMarker.GetIds();
			var botItems = GameIdGroup.BotItem.GetIds();
			var skinOptions = GameIdGroup.PlayerSkin.GetIds().Where(item => botItems.Contains(item)).ToArray();
			var botsTrophiesStep = f.GameConfig.BotsDifficultyTrophiesStep;
			var botsDifficulty = (int) FPMath.Floor((baseTrophiesAmount - 1000) / (FP) botsTrophiesStep);
			botsDifficulty = FPMath.Clamp(botsDifficulty, 0, f.GameConfig.BotsMaxDifficulty);

			var forcedBotTypes = new List<BotBehaviourType>();
			foreach (var playerSpawner in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				if (playerSpawner.Component->SpawnerType == SpawnerType.BotOfType)
				{
					forcedBotTypes.Add(playerSpawner.Component->BehaviourType);
				}
			}
			
			var botConfigsList = GetBotConfigsList(f, botsDifficulty);

			for (var i = 0; i < botsNameCount; i++)
			{
				botNamesIndices.Add(i + 1);
			}

			var playerCharacterPrototypeAsset =
				f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id);
			var navMeshAgentConfig = f.FindAsset<NavMeshAgentConfig>(f.AssetConfigs.BotNavMeshConfig.Id);


			var playersByTeam = new Dictionary<int, List<EntityRef>>();
			foreach (var player in f.Unsafe.GetComponentBlockIterator<PlayerCharacter>())
			{
				var teamId = player.Component->TeamId;
				if (teamId > 0)
				{
					if (!playersByTeam.TryGetValue(teamId, out var entities))
					{
						entities = new List<EntityRef>();
						playersByTeam[teamId] = entities;
					}

					entities.Add(player.Entity);
				}
			}

			// Add missing teams for the bots
			uint totalTeams = f.Context.GameModeConfig.MaxPlayers / (f.Context.GameModeConfig.Teams ? f.Context.GameModeConfig.MaxPlayersInTeam : 1);
			int currentTeams = playersByTeam.Count;
			for (int i = 0; i < totalTeams - currentTeams; i++)
			{
				playersByTeam.Add(Constants.TEAM_ID_START_BOT_PARTIES + i, new List<EntityRef>());
			}

			foreach (var id in botIds)
			{
				var teamId = this.GetBotTeamId(f, id, playersByTeam);

				var rngBotConfigIndex = f.RNG->Next(0, botConfigsList.Count);
				var botConfig = botConfigsList[rngBotConfigIndex];

				// If there are spawns for specific types of bots, we use those
				var forcedTypeConfigIndex = -1;
				while (forcedBotTypes.Count > 0 && forcedTypeConfigIndex == -1)
				{
					forcedTypeConfigIndex = botConfigsList.FindIndex(config => config.BehaviourType == forcedBotTypes[0]);
					forcedBotTypes.RemoveAt(0);
				}
				if (forcedTypeConfigIndex > -1)
				{
					botConfig = botConfigsList[forcedTypeConfigIndex];
				}
				
				var withPlayer = false;
				Transform3D spawnerTransform;
				var rngSpawnIndex = 0;
				if (playersByTeam.TryGetValue(teamId, out var teamMembers) && teamMembers.Count > 0)
				{
					spawnerTransform = f.Get<Transform3D>(teamMembers.First());
					spawnerTransform.Position.Y -= FP._2;
					withPlayer = true;
				}
				else
				{
					rngSpawnIndex = GetSpawnPointIndexByTypeOfBot(f, playerSpawners, botConfig.BehaviourType);
					spawnerTransform = f.Get<Transform3D>(playerSpawners[rngSpawnIndex].Entity);
				}


				var botEntity = f.Create(playerCharacterPrototypeAsset);
				var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
				var navMeshAgent = new NavMeshSteeringAgent();
				var pathfinder = NavMeshPathfinder.Create(f, botEntity, navMeshAgentConfig);
				var listNamesIndex = f.RNG->Next(0, botNamesIndices.Count);

				if (!playersByTeam.TryGetValue(teamId, out var entities))
				{
					entities = new List<EntityRef>();
					playersByTeam[teamId] = entities;
				}

				entities.Add(botEntity);
				var botCharacter = new BotCharacter
				{
					Skin = skinOptions[f.RNG->Next(0, skinOptions.Length)],
					DeathMarker = deathMakers[f.RNG->Next(0, deathMakers.Count)],
					BotNameIndex = botNamesIndices[listNamesIndex],
					BehaviourType = botConfig.BehaviourType,
					// We modify intervals to make them more unique to avoid performance spikes
					DecisionInterval = botConfig.DecisionInterval +
						botNamesIndices[listNamesIndex] * FP._0_01 * FP._0_10,
					LookForTargetsToShootAtInterval = botConfig.LookForTargetsToShootAtInterval +
						botNamesIndices[listNamesIndex] * FP._0_01 * FP._0_01,
					VisionRangeSqr = botConfig.VisionRangeSqr,
					LowArmourSensitivity = botConfig.LowArmourSensitivity,
					LowHealthSensitivity = botConfig.LowHealthSensitivity,
					LowAmmoSensitivity = botConfig.LowAmmoSensitivity,
					ChanceToSeekWeapons = botConfig.ChanceToSeekWeapons,
					ChanceToSeekEnemies = botConfig.ChanceToSeekEnemies,
					ChanceToSeekRage = botConfig.ChanceToSeekRage,
					ChanceToAbandonTarget = botConfig.ChanceToAbandonTarget,
					ChanceToSeekReplenishSpecials = botConfig.ChanceToSeekReplenishSpecials,
					CloseFightIntolerance = botConfig.CloseFightIntolerance,
					WanderRadius = botConfig.WanderRadius,
					AccuracySpreadAngle = botConfig.AccuracySpreadAngle,
					ChanceToUseSpecial = botConfig.ChanceToUseSpecial,
					SpecialAimingDeviation = botConfig.SpecialAimingDeviation,
					ShrinkingCircleRiskTolerance = botConfig.ShrinkingCircleRiskTolerance,
					ChanceToSeekChests = botConfig.ChanceToSeekChests,
					NextDecisionTime = FP._0,
					NextLookForTargetsToShootAtTime = FP._0,
					CurrentEvasionStepEndTime = FP._0,
					StuckDetectionPosition = FPVector3.Zero,
					LoadoutGearNumber = botConfig.LoadoutGearNumber,
					MaxAimingRange = botConfig.MaxAimingRange,
					MovementSpeedMultiplier = botConfig.MovementSpeedMultiplier,
					TeamSize = teamSize,
					MaxDistanceToTeammateSquared = botConfig.MaxDistanceToTeammateSquared,
					SpawnWithPlayer = withPlayer,
					DamageTakenMultiplier = botConfig.DamageTakenMultiplier,
					DamageDoneMultiplier = botConfig.DamageDoneMultiplier
				};

				botNamesIndices.RemoveAt(listNamesIndex);

				// Remove a spawner from list when we took a new one for another team; update stored teamId
				if (!withPlayer && playerSpawners.Count > 1)
				{
					playerSpawners.RemoveAt(rngSpawnIndex);
				}

				f.Add(botEntity, pathfinder); // Must be defined before the steering agent
				f.Add(botEntity, navMeshAgent);
				f.Add(botEntity, botCharacter);

				// Calculate bot trophies
				// TODO: Uncomment the old way of calculating trophies when we make Visual Trophies and Hidden Trophies
				// var trophies = (uint) ((botsDifficulty * botsTrophiesStep) + 1000 + f.RNG->Next(-50, 50));
				var trophies = (uint) (baseTrophiesAmount + f.RNG->Next(-50, 50));

				// TODO: Give bots random weapon based on median quality that players have
				// TODO: Give bots random gear in their loadout initially based on median quality that players have

				// array of test equipment for testing purposes
/*				var equipmentRarity = EquipmentRarity.Rare;
				var loadout = new Equipment[]
				{
					new Equipment(GameId.RiotHelmet, EquipmentEdition.Genesis, equipmentRarity),
					new Equipment(GameId.RiotAmulet, EquipmentEdition.Genesis, equipmentRarity),
					new Equipment(GameId.RiotArmor, EquipmentEdition.Genesis, equipmentRarity),
					new Equipment(GameId.RiotShield, EquipmentEdition.Genesis, equipmentRarity),
				};*/


				List<Modifier> modifiers = null;

				if (botConfig.DamageDoneMultiplier != FP._1 || botConfig.DamageTakenMultiplier != FP._1)
				{
					modifiers = new List<Modifier>();

					if (botConfig.DamageTakenMultiplier != FP._1)
					{
						modifiers.Add(new Modifier
						{
							Id = ++f.Global->ModifierIdCount,
							Type = StatType.Armour,
							OpType = OperationType.Add,
							Power = FP._100 * (botConfig.DamageTakenMultiplier - 1),
							Duration = FP.MaxValue,
							StartTime = FP._0,
							IsNegative = true
						});
					}

					if (botConfig.DamageDoneMultiplier != FP._1)
					{
						modifiers.Add(new Modifier
						{
							Id = ++f.Global->ModifierIdCount,
							Type = StatType.Power,
							OpType = OperationType.Multiply,
							Power = FP._1 - botConfig.DamageDoneMultiplier,
							Duration = FP.MaxValue,
							StartTime = FP._0,
							IsNegative = true
						});
					}
				}


				playerCharacter->Init(f, botEntity, id, spawnerTransform, 1, trophies, botCharacter.Skin,
					botCharacter.DeathMarker, teamId, Array.Empty<Equipment>(), Equipment.None, modifiers);
			}
		}

		private int GetBotTeamId(Frame frame, PlayerRef bot, Dictionary<int, List<EntityRef>> playerByTeam)
		{
			if (!frame.Context.GameModeConfig.Teams)
			{
				return bot;
			}

			var @override = frame.Context.GameModeConfig.BotsTeamOverride;
			if (@override != 0)
			{
				return @override;
			}

			var maxPlayers = frame.Context.GameModeConfig.MaxPlayersInTeam;
			foreach (var kv in playerByTeam)
			{
				if (kv.Value.Count < maxPlayers)
				{
					return kv.Key;
				}
			}

			return bot;
		}

		private List<EntityComponentPointerPair<PlayerSpawner>> GetFreeSpawnPoints(Frame f)
		{
			var list = new List<EntityComponentPointerPair<PlayerSpawner>>();
			var entity = EntityRef.None;

			foreach (var pair in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				if (f.Time < pair.Component->ActivationTime)
				{
					entity = !entity.IsValid ||
						f.Get<PlayerSpawner>(entity).ActivationTime > pair.Component->ActivationTime
							? pair.Entity
							: entity;
					continue;
				}

				list.Add(pair);
			}

			if (list.Count == 0)
			{
				list.Add(new EntityComponentPointerPair<PlayerSpawner>
				{
					Component = f.Unsafe.GetPointer<PlayerSpawner>(entity),
					Entity = entity
				});
			}

			return list;
		}

		private int GetSpawnPointIndexByTypeOfBot(Frame f, List<EntityComponentPointerPair<PlayerSpawner>> spawnPoints, BotBehaviourType botType)
		{
			// Try to find spawners that are specific to the type of bot
			var specificSpawnPoints = new List<int>();
			for (int i = 0; i < spawnPoints.Count; i++)
			{
				var playerSpawner = spawnPoints[i].Component;
				if (playerSpawner->SpawnerType == SpawnerType.BotOfType && playerSpawner->BehaviourType == botType)
				{
					specificSpawnPoints.Add(i);
				}
			}

			if (specificSpawnPoints.Count > 0)
			{
				return specificSpawnPoints[f.RNG->Next(0, specificSpawnPoints.Count)];
			}

			return f.RNG->Next(0, spawnPoints.Count);
		}
	}
}