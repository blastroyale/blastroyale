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
	                                         ISignalOnPlayerDataSet
	{
		public struct BotCharacterFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public BotCharacter* BotCharacter;
			public PlayerCharacter* PlayerCharacter;
		}

		/// <inheritdoc />
		public void OnPlayerDataSet(Frame f, PlayerRef playerRef)
		{
			if (f.ComponentCount<BotCharacter>() > 0)
			{
				return;
			}

			var playerLimit = f.PlayerCount;
			var botIds = new List<PlayerRef>();

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
				AddBots(f, botIds, playerRef);
			}
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref BotCharacterFilter filter)
		{
			// If it's a deathmatch game mode and a bot is dead then we process respawn behaviour
			if (f.Context.MapConfig.GameMode == GameMode.Deathmatch
			    && f.TryGet<DeadPlayerCharacter>(filter.Entity, out var deadBot))
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
			var weaponConfig = f.WeaponConfigs.GetConfig(filter.PlayerCharacter->CurrentWeapon.GameId);
			
			if (QuantumHelpers.IsDestroyed(f, target))
			{
				ClearTarget(f, ref filter);
			}
			else
			{
				kcc->MaxSpeed = speed * weaponConfig.AimingMovementSpeed;
				QuantumHelpers.LookAt2d(f, filter.Entity, target);
			}

			// Bots look for others to shoot at not on every frame
			if (f.Time > filter.BotCharacter->NextLookForTargetsToShootAtTime)
			{
				var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);
			
				profiler.Start("TryToShoot");
				
				// Bots also have a ChanceToAbandonTarget to stop shooting/tracking the target to allow more room for players to escape
				if (f.RNG->Next() < filter.BotCharacter->ChanceToAbandonTarget)
				{
					ClearTarget(f, ref filter);
				}
				else
				{
					// Check distance to target if it exists and stop the movement if target is closer
					// than allowed by CloseFightIntolerance
					if (!QuantumHelpers.IsDestroyed(f, target))
					{
						// Checking how close is the target and stop the movement if the target is closer
						// than allowed by closefight intolerance
						var weaponTargetRange = weaponConfig.AttackRange;
						var minDistanceToTarget =
							FPMath.Max(FP._1, weaponTargetRange * filter.BotCharacter->CloseFightIntolerance);
						var sqrDistanceToTarget = (f.Get<Transform3D>(target).Position - filter.Transform->Position)
							.SqrMagnitude;
						if (sqrDistanceToTarget < minDistanceToTarget * minDistanceToTarget)
						{
							f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity)->Stop(f, filter.Entity, true);
						}
					}

					CheckEnemiesToShooAt(f, ref filter, weaponConfig);
				}

				filter.BotCharacter->NextLookForTargetsToShootAtTime =
					f.Time + filter.BotCharacter->LookForTargetsToShootAtInterval;
				
				profiler.End();
			}

			// Do not do any decision making if the time has not come
			if (f.Time < filter.BotCharacter->NextDecisionTime)
			{
				return;
			}

			filter.BotCharacter->NextDecisionTime = f.Time + filter.BotCharacter->DecisionInterval + filter.BotCharacter->BotNameIndex*FP._0_01;

			if (TryUseSpecials(f, ref filter))
			{
				ClearTarget(f, ref filter);
			}

			switch (filter.BotCharacter->BehaviourType)
			{
				case BotBehaviourType.Cautious:
					var cautious = TryAvoidShrinkingCircle(f, ref filter)
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
					                 || TryGoForRage(f, ref filter)
					                 || TryGoForWeapons(f, ref filter)
					                 || TryGoForEnemies(f, ref filter, weaponConfig)
					                 || TryGoForCrates(f, ref filter)
					                 || TryGoForAmmo(f, ref filter)
					                 || TryGoForShield(f, ref filter)
					                 || TryGoForHealth(f, ref filter)
					                 || Wander(f, ref filter);
					break;
				case BotBehaviourType.Balanced:
					var balanced = TryAvoidShrinkingCircle(f, ref filter)
					               || TryGoForAmmo(f, ref filter)
					               || TryGoForHealth(f, ref filter)
					               || TryGoForWeapons(f, ref filter)
					               || TryGoForCrates(f, ref filter)
					               || TryGoForShield(f, ref filter)
					               || TryGoForEnemies(f, ref filter, weaponConfig)
					               || TryGoForRage(f, ref filter)
					               || Wander(f, ref filter);
					break;
			}
		}

		private List<QuantumBotConfig> GetBotConfigsList(Frame f)
		{
			var list = new List<QuantumBotConfig>();
			var configs = f.BotConfigs.QuantumConfigs;
			var difficultyLevel = Constants.BOT_DIFFICULTY_LEVEL;

			foreach (var botConfig in configs)
			{
				if (botConfig.Difficulty == difficultyLevel && botConfig.GameMode == f.Context.MapConfig.GameMode)
				{
					list.Add(botConfig);
				}
			}

			return list;
		}

		private void ClearTarget(Frame f, ref BotCharacterFilter filter)
		{
			var speed = f.Get<Stats>(filter.Entity).Values[(int) StatType.Speed].StatValue;

			filter.BotCharacter->Target = EntityRef.None;

			// When we clear the target we also return speed to normal
			// because without a target bots don't shoot
			f.Unsafe.GetPointer<CharacterController3D>(filter.Entity)->MaxSpeed = speed;
		}

		private void CheckEnemiesToShooAt(Frame f, ref BotCharacterFilter filter, QuantumWeaponConfig weaponConfig)
		{
			var target = EntityRef.None;

			// If the bot's weapon is empty then we clear the target and leave the method
			if (filter.PlayerCharacter->IsAmmoEmpty(f, filter.Entity))
			{
				filter.BotCharacter->Target = EntityRef.None;
				return;
			}

			// Otherwise, we do line/shapecasts for enemies in sight
			// If there is a target in Sight then store this Target into the blackboard variable
			// We check enemies one by one until we find a valid enemy in sight
			// TODO: Select not a random, but the closest possible enemy to shoot at
			var targetRange = weaponConfig.AttackRange;
			var botPosition = filter.Transform->Position;
			var team = f.Get<Targetable>(filter.Entity).Team;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);

			botPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

			foreach (var targetCandidate in f.GetComponentIterator<Targetable>())
			{
				if (!QuantumHelpers.IsAttackable(f, targetCandidate.Entity, team) ||
				    !QuantumHelpers.IsEntityInRange(f, filter.Entity, targetCandidate.Entity, FP._0, targetRange))
				{
					continue;
				}

				var targetPosition = f.Get<Transform3D>(targetCandidate.Entity).Position;
				targetPosition.Y += Constants.ACTOR_AS_TARGET_Y_OFFSET;

				var hit = f.Physics3D.Linecast(botPosition,
				                               targetPosition,
				                               f.Context.TargetAllLayerMask,
				                               QueryOptions.HitDynamics | QueryOptions.HitStatics |
				                               QueryOptions.HitKinematics);

				if (hit.HasValue)
				{
					target = hit.Value.Entity;

					bb->Set(f, Constants.AimDirectionKey, (targetPosition - botPosition).XZ);
					break;
				}
			}

			filter.BotCharacter->Target = target;

			bb->Set(f, Constants.IsAimingKey, target != EntityRef.None);
		}

		// We check specials and try to use them depending on their type if possible
		private bool TryUseSpecials(Frame f, ref BotCharacterFilter filter)
		{
			if (!(f.RNG->Next() < filter.BotCharacter->ChanceToUseSpecial))
			{
				return false;
			}

			var target = filter.BotCharacter->Target;

			for (var specialIndex = 0; specialIndex < Constants.MAX_SPECIALS; specialIndex++)
			{
				var special = filter.PlayerCharacter->Specials.GetPointer(specialIndex);

				if ((target != EntityRef.None || special->SpecialType == SpecialType.ShieldSelfStatus) &&
				    special->IsValid && special->TryActivate(f, filter.Entity, FPVector2.Zero, specialIndex))
				{
					return true;
				}
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
			
			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity);

			if (agent->IsActive && filter.BotCharacter->MoveTarget == EntityRef.None)
			{
				return true;
			}
			
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);

			profiler.Start("TryAvoidShrinkingCircle");

			var sqrDistanceFromSafeAreaCenter =
				FPVector2.DistanceSquared(filter.Transform->Position.XZ, circle.TargetCircleCenter);
			var sqrSafeAreaRadius = circle.TargetRadius * circle.TargetRadius;
			var safeCircleCenter = circle.TargetCircleCenter.XOY;
			safeCircleCenter.Y = filter.Transform->Position.Y;

			var direction = filter.Transform->Position - safeCircleCenter;
			direction = direction.Normalized * filter.BotCharacter->WanderRadius * 3;

			// If sqrDistanceFromSafeAreaCenter / sqrSafeAreaRadius > 1 then the bot is outside the safe area
			// If sqrDistanceFromSafeAreaCenter / sqrSafeAreaRadius < 1 then the bot is safe, inside safe area

			var isGoing = sqrDistanceFromSafeAreaCenter / sqrSafeAreaRadius >
			              filter.BotCharacter->ShrinkingCircleRiskTolerance;

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, direction,
			                                                     filter.BotCharacter->WanderRadius);
			filter.BotCharacter->MoveTarget = EntityRef.None;
			

			profiler.End();
			
			return isGoing;
		}

		private bool TryGoForRage(Frame f, ref BotCharacterFilter filter)
		{
			if (!(f.RNG->Next() < filter.BotCharacter->ChanceToSeekRage))
			{
				return false;
			}

			
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);

			profiler.Start("TryGoForRage");

			var isGoing = TryGetClosestConsumable(f, ref filter, ConsumableType.Rage, out var rageConsumablePosition, out var rageConsumableEntity);

			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity);
			if (agent->IsActive && filter.BotCharacter->MoveTarget == rageConsumableEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, rageConsumablePosition);
			filter.BotCharacter->MoveTarget = rageConsumableEntity;

			profiler.End();
			
			return isGoing;
		}

		private bool TryGoForShield(Frame f, ref BotCharacterFilter filter)
		{
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);
			
			profiler.Start("TryGoForShield");
			
			var armourConsumablePosition = FPVector3.Zero;
			var armourConsumableEntity = EntityRef.None;
			
			var stats = f.Get<Stats>(filter.Entity);
			var maxArmour = stats.Values[(int) StatType.Shield].StatValue;
			var ratioArmour = stats.CurrentShield / maxArmour;
			var lowArmourSensitivity = filter.BotCharacter->LowArmourSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioArmour) * lowArmourSensitivity);

			isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.Shield,
			                                             out armourConsumablePosition, out armourConsumableEntity);

			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity);
			if (agent->IsActive && filter.BotCharacter->MoveTarget == armourConsumableEntity)
			{
				return true;
			}
			
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, armourConsumablePosition);
			filter.BotCharacter->MoveTarget = armourConsumableEntity;
			
			profiler.End();
			
			return isGoing;
		}

		private bool TryGoForHealth(Frame f, ref BotCharacterFilter filter)
		{
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);
			
			profiler.Start("TryGoForHealth");
			
			var healthConsumablePosition = FPVector3.Zero;
			var healthConsumableEntity = EntityRef.None;
			var stats = f.Get<Stats>(filter.Entity);
			var maxHealth = stats.Values[(int) StatType.Health].StatValue;
			var ratioHealth = stats.CurrentHealth / maxHealth;
			var lowHealthSensitivity = filter.BotCharacter->LowHealthSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioHealth) * lowHealthSensitivity);

			isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.Health,
			                                             out healthConsumablePosition, out healthConsumableEntity);

			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity);
			if (agent->IsActive && filter.BotCharacter->MoveTarget == healthConsumableEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, healthConsumablePosition);
			filter.BotCharacter->MoveTarget = healthConsumableEntity;

			profiler.End();
			
			return isGoing;
		}

		private bool TryGoForAmmo(Frame f, ref BotCharacterFilter filter)
		{
			var ammoConsumablePosition = FPVector3.Zero;
			var ammoConsumableEntity = EntityRef.None;

			// If weapon has Unlimited ammo then don't go for more ammo
			if (filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity))
			{
				return false;
			}
			
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);
			
			profiler.Start("TryGoForAmmo");

			var ratioAmmo = filter.PlayerCharacter->GetAmmoAmountFilled(f, filter.Entity);
			var lowAmmoSensitivity = filter.BotCharacter->LowAmmoSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioAmmo) * lowAmmoSensitivity);

			isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.Ammo,
			                                             out ammoConsumablePosition, out ammoConsumableEntity);
			
			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity);
			if (agent->IsActive && filter.BotCharacter->MoveTarget == ammoConsumableEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, ammoConsumablePosition);
			filter.BotCharacter->MoveTarget = ammoConsumableEntity;

			profiler.End();
			
			return isGoing;
		}

		private bool TryGoForCrates(Frame f, ref BotCharacterFilter filter)
		{
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);
			
			profiler.Start("TryGoForCrates");
			
			var isGoing = TryGetClosestChest(f, ref filter, out var chestPosition, out var chestEntity);

			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity);
			if (agent->IsActive && filter.BotCharacter->MoveTarget == chestEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, chestPosition);
			filter.BotCharacter->MoveTarget = chestEntity;

			profiler.End();
			
			return isGoing;
		}

		private bool TryGoForWeapons(Frame f, ref BotCharacterFilter filter)
		{
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);
			
			profiler.Start("TryGoForWeapons");
			
			var weaponPickupPosition = FPVector3.Zero;
			var weaponPickupEntity = EntityRef.None;

			// Bots seek new weapons if they have a default one OR if they have no ammo OR if the chance worked
			var isGoing = filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) ||
			              filter.PlayerCharacter->IsAmmoEmpty(f, filter.Entity) ||
			              f.RNG->Next() < filter.BotCharacter->ChanceToSeekWeapons;

			isGoing = isGoing && TryGetClosestWeapon(f, ref filter, out weaponPickupPosition, out weaponPickupEntity);
			
			var agent = f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity);
			if (agent->IsActive && filter.BotCharacter->MoveTarget == weaponPickupEntity)
			{
				return true;
			}

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, weaponPickupPosition);
			filter.BotCharacter->MoveTarget = weaponPickupEntity;

			profiler.End();

			return isGoing;
		}

		private bool TryGoForEnemies(Frame f, ref BotCharacterFilter filter, QuantumWeaponConfig weaponConfig)
		{
			var isGoing = f.RNG->Next() < filter.BotCharacter->ChanceToSeekEnemies;

			// If chance didn't work OR the bot's weapon is empty then we don't go for enemies
			if (!isGoing || filter.PlayerCharacter->IsAmmoEmpty(f, filter.Entity))
			{
				return false;
			}
			
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);
			
			profiler.Start("TryGoForEnemies");

			var enemyPosition = FPVector3.Zero;
			var iterator = f.GetComponentIterator<Targetable>();
			var sqrDistance = FP.MaxValue;
			var team = f.Get<Targetable>(filter.Entity).Team;
			var botPosition = filter.Transform->Position;
			var enemyEntity = EntityRef.None;

			foreach (var targetCandidate in iterator)
			{
				if (!QuantumHelpers.IsAttackable(f, targetCandidate.Entity, team))
				{
					continue;
				}

				var positionCandidate = f.Get<Transform3D>(targetCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter) && newSqrDistance < sqrDistance)
				{
					sqrDistance = newSqrDistance;
					enemyPosition = positionCandidate;
					enemyEntity = targetCandidate.Entity;
				}
			}

			if (enemyPosition == FPVector3.Zero)
			{
				profiler.End();
				return false;
			}

			var weaponTargetRange = weaponConfig.AttackRange;
			var reverseDirection = (enemyPosition - botPosition).Normalized;
			// Do not go closer than 1 meter to target
			var offsetDistance = FPMath.Max(FP._1, weaponTargetRange * filter.BotCharacter->CloseFightIntolerance);
			var offsetPosition = enemyPosition + reverseDirection * offsetDistance;

			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, offsetPosition);
			filter.BotCharacter->MoveTarget = enemyEntity;

			profiler.End();

			return isGoing;
		}

		private bool Wander(Frame f, ref BotCharacterFilter filter)
		{
			var profiler = f.Context.ProfilerContext.GetProfilerForTaskThread((FrameThreadSafe)f);
			
			profiler.Start("Wander");
			
			// We make several attempts to find a random position to wander to
			// to minimize a chance of a situation where a bot stands in place doing nothing
			for (int i = 1; i < 5; i++)
			{
				if (QuantumHelpers.SetClosestTarget(f, filter.Entity, filter.Transform->Position,
				                                    filter.BotCharacter->WanderRadius*i))
				{
					profiler.End();
					return true;
				}
			}
			
			profiler.End();

			return false;
		}

		// Method to get consumable without a specific "consumablePowerAmount"
		private bool TryGetClosestConsumable(Frame f, ref BotCharacterFilter filter, ConsumableType consumableType,
		                                     out FPVector3 consumablePosition, out EntityRef consumableEntity)
		{
			return TryGetClosestConsumable(f, ref filter, consumableType, -1, out consumablePosition, out consumableEntity);
		}

		private bool TryGetClosestConsumable(Frame f, ref BotCharacterFilter filter, ConsumableType consumableType,
		                                     int consumablePowerAmount, out FPVector3 consumablePosition, out EntityRef consumableEntity)
		{
			var botPosition = filter.Transform->Position;
			var iterator = f.GetComponentIterator<Consumable>();
			var sqrDistance = FP.MaxValue;
			consumablePosition = FPVector3.Zero;
			consumableEntity = EntityRef.None;

			foreach (var consumableCandidate in iterator)
			{
				if (consumableCandidate.Component.ConsumableType != consumableType ||
				    consumablePowerAmount != -1 && consumablePowerAmount != consumableCandidate.Component.Amount)
				{
					continue;
				}

				var positionCandidate = f.Get<Transform3D>(consumableCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter) && newSqrDistance < sqrDistance)
				{
					sqrDistance = newSqrDistance;
					consumablePosition = positionCandidate;
					consumableEntity = consumableCandidate.Entity;
				}
			}

			return consumablePosition != FPVector3.Zero;
		}

		private bool TryGetClosestWeapon(Frame f, ref BotCharacterFilter filter, out FPVector3 weaponPickupPosition, out EntityRef weaponPickupEntity)
		{
			var botPosition = filter.Transform->Position;
			var iterator = f.GetComponentIterator<EquipmentCollectable>();
			var sqrDistance = FP.MaxValue;
			var totalAmmo = filter.PlayerCharacter->GetAmmoAmount(f, filter.Entity, out var maxAmmo);
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

				if (IsInVisionRange(newSqrDistance, ref filter) && newSqrDistance < sqrDistance)
				{
					sqrDistance = newSqrDistance;
					weaponPickupPosition = positionCandidate;
					weaponPickupEntity = weaponCandidate.Entity;
				}
			}

			return weaponPickupPosition != FPVector3.Zero;
		}

		private bool TryGetClosestChest(Frame f, ref BotCharacterFilter filter, out FPVector3 weaponPickupPosition, out EntityRef weaponPickupEntity)
		{
			// TODO mihak: Implement this
			
			weaponPickupPosition = FPVector3.Zero;
			weaponPickupEntity = EntityRef.None;
			return false;
		}

		private bool IsInVisionRange(FP distanceSqr, ref BotCharacterFilter filter)
		{
			var visionRangeSqr = filter.BotCharacter->VisionRangeSqr;

			return visionRangeSqr < FP._0 || distanceSqr <= visionRangeSqr;
		}

		private void AddBots(Frame f, List<PlayerRef> botIds, PlayerRef playerRef)
		{
			var playerSpawners = GetFreeSpawnPoints(f);
			var botConfigsList = GetBotConfigsList(f);
			var botNamesIndices = new List<int>();

			var skinOptions = GameIdGroup.PlayerSkin.GetIds()
			                             .Where(item => GameIdGroup.BotItem.GetIds().Contains(item)).ToArray();

			for (var i = 0; i < f.GameConfig.BotsNameCount; i++)
			{
				botNamesIndices.Add(i + 1);
			}

			foreach (var id in botIds)
			{
				var rngSpawnIndex = f.RNG->Next(0, playerSpawners.Count);
				var spawnerTransform = f.Get<Transform3D>(playerSpawners[rngSpawnIndex].Entity);
				var botEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
				var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
				var navMeshAgent = new NavMeshSteeringAgent();
				var pathfinder = NavMeshPathfinder.Create(f, botEntity,
				                                          f.FindAsset<NavMeshAgentConfig>(f.AssetConfigs
					                                          .BotNavMeshConfig.Id));
				var rngBotConfigIndex = f.RNG->Next(0, botConfigsList.Count);
				var botConfig = botConfigsList[rngBotConfigIndex];
				var listNamesIndex = f.RNG->Next(0, botNamesIndices.Count);

				var botCharacter = new BotCharacter
				{
					Skin = skinOptions[f.RNG->Next(0, skinOptions.Length)],
					BotNameIndex = botNamesIndices[listNamesIndex],
					BehaviourType = botConfig.BehaviourType,
					DecisionInterval = botConfig.DecisionInterval,
					LookForTargetsToShootAtInterval = botConfig.LookForTargetsToShootAtInterval,
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
					NextDecisionTime = FP._0,
					NextLookForTargetsToShootAtTime = FP._0,
				};

				botNamesIndices.RemoveAt(listNamesIndex);

				if (playerSpawners.Count > 1)
				{
					playerSpawners.RemoveAt(rngSpawnIndex);
				}

				f.Add(botEntity, pathfinder); // Must be defined before the steering agent
				f.Add(botEntity, navMeshAgent);
				f.Add(botEntity, botCharacter);

				// Calculate bot trophies
				var eloRange = f.GameConfig.TrophyEloRange;
				var playerTrophies = f.GetPlayerData(playerRef).PlayerTrophies;
				var trophies = (uint) Math.Max((int) playerTrophies + f.RNG->Next(-eloRange / 2, eloRange / 2), 0);

				playerCharacter->Init(f, botEntity, id, spawnerTransform, 1, trophies, botCharacter.Skin, Array.Empty<Equipment>(), Equipment.None);
			}
		}

		public List<EntityComponentPointerPair<PlayerSpawner>> GetFreeSpawnPoints(Frame f)
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
	}
}