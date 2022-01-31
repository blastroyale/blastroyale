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
		}

		/// <inheritdoc />
		public void OnPlayerDataSet(Frame f, PlayerRef playerRef)
		{
			if (f.ComponentCount<BotCharacter>() > 0)
			{
				return;
			}
			
			var playerLimit = f.RuntimeConfig.PlayersLimit;
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
				AddBots(f, botIds);
			}
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref BotCharacterFilter filter)
		{
			// If it's a deathmatch game mode and a bot is dead then we process respawn behaviour
			if (f.RuntimeConfig.GameMode == GameMode.Deathmatch
			    && f.TryGet<DeadPlayerCharacter>(filter.Entity, out var deadBot))
			{
				// If the bot is dead and it's not yet the time to respawn then we skip the update
				if (f.Time < deadBot.TimeOfDeath + f.GameConfig.PlayerRespawnTime)
				{
					return;
				}
				
				var spawnPoint = QuantumHelpers.GetPlayerSpawnTransform(f);
				
				f.Unsafe.GetPointer<PlayerCharacter>(filter.Entity)->Spawn(f, filter.Entity, spawnPoint.Component, true);
			}
			
			// If a bot is not alive OR a bot is stunned 
			// then we don't go further with the behaviour
			if (!f.Has<AlivePlayerCharacter>(filter.Entity) || f.Has<Stun>(filter.Entity))
			{
				return;
			}
			
			// If a bot has a valid target then we correct the bot's speed according
			// to the weapon they carry and turn the bot towards the target
			// otherwise we return speed to normal and let automatic navigation turn the bot
			var target = filter.BotCharacter->Target;
			var speed = f.Get<Stats>(filter.Entity).Values[(int) StatType.Speed].StatValue;
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);
			var weapon = f.Get<Weapon>(filter.Entity);
			
			if (QuantumHelpers.IsDestroyed(f, target))
			{
				ClearTarget(f, ref filter);
			}
			else
			{
				kcc->MaxSpeed = speed * weapon.AimingMovementSpeed;
				QuantumHelpers.LookAt2d(f, filter.Entity, target);
			}
			
			// Bots look for others to shoot at not on every frame
			if (f.Time > filter.BotCharacter->NextLookForTargetsToShootAtTime)
			{
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
						var weaponTargetRange = weapon.AttackRange;
						var minDistanceToTarget = FPMath.Max(FP._1, weaponTargetRange * filter.BotCharacter->CloseFightIntolerance);
						var sqrDistanceToTarget = (f.Get<Transform3D>(target).Position - filter.Transform->Position).SqrMagnitude;
						if (sqrDistanceToTarget < minDistanceToTarget * minDistanceToTarget)
						{
							f.Unsafe.GetPointer<NavMeshPathfinder>(filter.Entity)->Stop(f, filter.Entity, true);
						}
					}
					
					CheckEnemiesToShooAt(f, ref filter);
				}
				
				filter.BotCharacter->NextLookForTargetsToShootAtTime = f.Time + filter.BotCharacter->LookForTargetsToShootAtInterval;
			}
			
			// Do not do any decision making if the time has not come
			if (f.Time < filter.BotCharacter->NextDecisionTime)
			{
				return;
			}
			filter.BotCharacter->NextDecisionTime = f.Time + filter.BotCharacter->DecisionInterval;
			
			if (TryUseSpecials(f, ref filter))
			{
				ClearTarget(f, ref filter);
			}
			
			switch (filter.BotCharacter->BehaviourType)
			{
				case BotBehaviourType.Cautious:
					var cautious = TryAvoidShrinkingCircle(f, ref filter)
					               || TryGoForHealth(f, ref filter)
					               || TryGoForInterimArmour(f, ref filter)
					               || TryGoForAmmo(f, ref filter)
					               || TryGoForWeapons(f, ref filter)
					               || TryGoForCrates(f, ref filter)
					               || TryGoForRage(f, ref filter)
					               || TryGoForEnemies(f, ref filter)
					               || Wander(f, ref filter);
					break;
				case BotBehaviourType.Aggressive:
					var aggressive = TryAvoidShrinkingCircle(f, ref filter)
					               || TryGoForRage(f, ref filter)
					               || TryGoForWeapons(f, ref filter)
					               || TryGoForEnemies(f, ref filter)
					               || TryGoForCrates(f, ref filter)
					               || TryGoForAmmo(f, ref filter)
					               || TryGoForInterimArmour(f, ref filter)
					               || TryGoForHealth(f, ref filter)
					               || Wander(f, ref filter);
					break;
				case BotBehaviourType.Balanced:
					var balanced = TryAvoidShrinkingCircle(f, ref filter)
					               || TryGoForAmmo(f, ref filter)
					               || TryGoForHealth(f, ref filter)
					               || TryGoForWeapons(f, ref filter)
					               || TryGoForCrates(f, ref filter)
					               || TryGoForInterimArmour(f, ref filter)
					               || TryGoForEnemies(f, ref filter)
					               || TryGoForRage(f, ref filter)
					               || Wander(f, ref filter);
					break;
			}
		}
		
		private List<QuantumBotConfig> GetBotConfigsList(Frame f)
		{
			var list = new List<QuantumBotConfig>();
			var configs = f.BotConfigs.QuantumConfigs;
			var difficultyLevel = f.RuntimeConfig.BotDifficultyLevel;

			foreach (var botConfig in configs)
			{
				if (botConfig.Difficulty == difficultyLevel)
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
		
		private void CheckEnemiesToShooAt(Frame f, ref BotCharacterFilter filter)
		{
			var target = EntityRef.None;
			var weapon = f.Get<Weapon>(filter.Entity);
			
			// If the bot's weapon is empty then we clear the target and leave the method
			if (weapon.Ammo == 0)
			{
				filter.BotCharacter->Target = EntityRef.None;
				return;
			}
			
			// Otherwise, we do line/shapecasts for enemies in sight
			// If there is a target in Sight then store this Target into the blackboard variable
			// We check enemies one by one until we find a valid enemy in sight
			// TODO: Select not a random, but the closest possible enemy to shoot at
			var targetRange = weapon.AttackRange;
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
				                               f.TargetAllLayerMask,
				                               QueryOptions.HitDynamics | QueryOptions.HitStatics | QueryOptions.HitKinematics);

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
			
			var weaponPointer = f.Unsafe.GetPointer<Weapon>(filter.Entity);
			var target = filter.BotCharacter->Target;
			
			for (var specialIndex = 0; specialIndex < Constants.MAX_SPECIALS; specialIndex++)
			{
				var special = weaponPointer->Specials.GetPointer(specialIndex);
				
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
			
			var sqrDistanceFromShrinkingCenter =
				FPVector2.DistanceSquared(filter.Transform->Position.XZ, circle.CurrentCircleCenter);
			var sqrShrinkingRadius = circle.CurrentRadius * circle.CurrentRadius;
			var safeCircleCenter = circle.TargetCircleCenter.XOY;
			safeCircleCenter.Y = filter.Transform->Position.Y;
			
			// If sqrDistanceFromShrinkingCenter / sqrShrinkingRadius > 1 then the bot is outside the shrinking circle
			// If sqrDistanceFromShrinkingCenter / sqrShrinkingRadius < 1 then the bot is safe, inside the shrinking circle
			
			var isGoing = sqrDistanceFromShrinkingCenter / sqrShrinkingRadius > filter.BotCharacter->ShrinkingCircleRiskTolerance;
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, safeCircleCenter, circle.TargetRadius);
			
			return isGoing;
		}

		private bool TryGoForRage(Frame f, ref BotCharacterFilter filter)
		{
			if (!(f.RNG->Next() < filter.BotCharacter->ChanceToSeekRage))
			{
				return false;
			}
			
			var isGoing = TryGetClosestConsumable(f, ref filter, ConsumableType.Rage, out var rageConsumablePosition);
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, rageConsumablePosition);
			
			return isGoing;
		}
		
		private bool TryGoForInterimArmour(Frame f, ref BotCharacterFilter filter)
		{
			var armourConsumablePosition = FPVector3.Zero;
			var stats = f.Get<Stats>(filter.Entity);
			var maxArmour = stats.Values[(int) StatType.InterimArmour].StatValue;
			var ratioArmour = stats.CurrentInterimArmour / maxArmour;
			var lowArmourSensitivity = filter.BotCharacter->LowArmourSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioArmour) * lowArmourSensitivity);
			
			isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.InterimArmour, out armourConsumablePosition);
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, armourConsumablePosition);
			
			return isGoing;
		}
		
		private bool TryGoForHealth(Frame f, ref BotCharacterFilter filter)
		{
			var healthConsumablePosition = FPVector3.Zero;
			var stats = f.Get<Stats>(filter.Entity);
			var maxHealth = stats.Values[(int) StatType.Health].StatValue;
			var ratioHealth = stats.CurrentHealth / maxHealth;
			var lowHealthSensitivity = filter.BotCharacter->LowHealthSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioHealth) * lowHealthSensitivity);
			
			isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.Health, out healthConsumablePosition);
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, healthConsumablePosition);
			
			return isGoing;
		}
		
		private bool TryGoForAmmo(Frame f, ref BotCharacterFilter filter)
		{
			var ammoConsumablePosition = FPVector3.Zero;
			var weapon = f.Get<Weapon>(filter.Entity);
			
			// If weapon has Unlimited ammo then don't go for more ammo
			if (weapon.Ammo == -1)
			{
				return false;
			}
			
			var ratioAmmo = weapon.Ammo / weapon.MaxAmmo;
			var lowAmmoSensitivity = filter.BotCharacter->LowAmmoSensitivity;
			var isGoing = f.RNG->Next() < FPMath.Clamp01((FP._1 - ratioAmmo) * lowAmmoSensitivity);
			
			isGoing = isGoing && TryGetClosestConsumable(f, ref filter, ConsumableType.Ammo, out ammoConsumablePosition);
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, ammoConsumablePosition);
			
			return isGoing;
		}
		
		private bool TryGoForCrates(Frame f, ref BotCharacterFilter filter)
		{
			var cratePosition = FPVector3.Zero;
			
			var isGoing = TryGetClosestConsumable(f, ref filter, ConsumableType.Stash, out cratePosition);
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, cratePosition);
			
			return isGoing;
		}
		
		private bool TryGoForWeapons(Frame f, ref BotCharacterFilter filter)
		{
			var weaponPickupPosition = FPVector3.Zero;
			var weapon = f.Get<Weapon>(filter.Entity);
			
			// Bots seek new weapons if they have a default one OR if they have no ammo OR if the chance worked
			var isGoing = weapon.WeaponId == Constants.DEFAULT_WEAPON_GAME_ID ||
			              weapon.Ammo == 0 ||
			              f.RNG->Next() < filter.BotCharacter->ChanceToSeekWeapons;
			
			isGoing = isGoing && TryGetClosestWeapon(f, ref filter, out weaponPickupPosition);
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, weaponPickupPosition);
			
			return isGoing;
		}
		
		private bool TryGoForEnemies(Frame f, ref BotCharacterFilter filter)
		{
			var isGoing = f.RNG->Next() < filter.BotCharacter->ChanceToSeekEnemies;
			
			// If chance didn't work OR the bot's weapon is empty then we don't go for enemies
			if (!isGoing || f.TryGet<Weapon>(filter.Entity, out var weapon) && weapon.Ammo == 0)
			{
				return false;
			}
			
			var enemyPosition = FPVector3.Zero;
			var iterator = f.GetComponentIterator<Targetable>();
			var sqrDistance = FP.MaxValue;
			var team = f.Get<Targetable>(filter.Entity).Team;
			var botPosition = filter.Transform->Position;
			
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
				}
			}
			
			if (enemyPosition == FPVector3.Zero)
			{
				return false;
			}
			
			var weaponTargetRange = weapon.AttackRange;
			var reverseDirection = (enemyPosition - botPosition).Normalized;
			// Do not go closer than 1 meter to target
			var offsetDistance = FPMath.Max(FP._1, weaponTargetRange * filter.BotCharacter->CloseFightIntolerance);
			var offsetPosition = enemyPosition + reverseDirection * offsetDistance;
			
			isGoing = isGoing && QuantumHelpers.SetClosestTarget(f, filter.Entity, offsetPosition);
			
			return isGoing;
		}
		
		private bool Wander(Frame f, ref BotCharacterFilter filter)
		{
			// We make several attempts to find a random position to wander to
			// to minimize a chance of a situation where a bot stands in place doing nothing
			for (int i = 0; i < 4; i++)
			{
				if (QuantumHelpers.SetClosestTarget(f, filter.Entity, filter.Transform->Position,
				                                    filter.BotCharacter->WanderRadius))
				{
					return true;
				}
			}
			
			return false;
		}

		// Method to get consumable without a specific "consumablePowerAmount"
		private bool TryGetClosestConsumable(Frame f, ref BotCharacterFilter filter, ConsumableType consumableType, out FPVector3 consumablePosition)
		{
			return TryGetClosestConsumable(f, ref filter, consumableType, -1, out consumablePosition);
		}

		private bool TryGetClosestConsumable(Frame f, ref BotCharacterFilter filter, ConsumableType consumableType, 
		                                     int consumablePowerAmount, out FPVector3 consumablePosition)
		{
			var botPosition = filter.Transform->Position;
			var iterator = f.GetComponentIterator<Consumable>();
			var sqrDistance = FP.MaxValue;
			consumablePosition = FPVector3.Zero;
			
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
				}
			}
			
			return consumablePosition != FPVector3.Zero;
		}

		private bool TryGetClosestWeapon(Frame f, ref BotCharacterFilter filter, out FPVector3 weaponPickupPosition)
		{
			var weapon = f.Get<Weapon>(filter.Entity);
			var botPosition = filter.Transform->Position;
			var iterator = f.GetComponentIterator<WeaponCollectable>();
			var sqrDistance = FP.MaxValue;
			weaponPickupPosition = FPVector3.Zero;
			
			foreach (var weaponCandidate in iterator)
			{
				var weaponCandidateId = f.Get<Collectable>(weaponCandidate.Entity).GameId;
				
				// Do not pick up the same weapon unless has less than 50% ammo
				if (weapon.WeaponId == weaponCandidateId && weapon.Ammo > weapon.MaxAmmo * FP._0_50)
				{
					continue;
				}
				
				var positionCandidate = f.Get<Transform3D>(weaponCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;
				
				if (IsInVisionRange(newSqrDistance, ref filter) && newSqrDistance < sqrDistance)
				{
					sqrDistance = newSqrDistance;
					weaponPickupPosition = positionCandidate;
				}
			}
			
			return weaponPickupPosition != FPVector3.Zero;
		}
		
		private bool IsInVisionRange(FP distanceSqr, ref BotCharacterFilter filter)
		{
			var visionRangeSqr = filter.BotCharacter->VisionRangeSqr;
			
			return visionRangeSqr < FP._0 || distanceSqr <= visionRangeSqr;
		}
		
		private void AddBots(Frame f, List<PlayerRef> botIds)
		{
			var playerSpawners = GetFreeSpawnPoints(f);
			var skins = GameIdGroup.PlayerSkin.GetIds();
			var botConfigsList = GetBotConfigsList(f);
			var botNamesIndices = new List<int>();
			
			for (var i = 0; i < f.GameConfig.BotsNameCount; i++)
			{
				botNamesIndices.Add(i+1);
			}
			
			foreach (var id in botIds)
			{
				var rngSpawnIndex = f.RNG->Next(0, playerSpawners.Count);
				var spawnerTransform = f.Get<Transform3D>(playerSpawners[rngSpawnIndex].Entity);
				var weaponConfig = f.WeaponConfigs.GetConfig(Constants.DEFAULT_WEAPON_GAME_ID);
				var botEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
				var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
				var gear = new Equipment[Constants.EQUIPMENT_SLOT_COUNT];
				var navMeshAgent = new NavMeshSteeringAgent();
				var pathfinder = NavMeshPathfinder.Create(f, botEntity, f.FindAsset<NavMeshAgentConfig>(f.AssetConfigs.BotNavMeshConfig.Id));
				var rngBotConfigIndex = f.RNG->Next(0, botConfigsList.Count);
				var botConfig = botConfigsList[rngBotConfigIndex];
				var listNamesIndex = f.RNG->Next(0, botNamesIndices.Count);
				var gearSlots = Constants.GearSlots.ToList();
				var gearPieces = botConfig.Difficulty < Constants.EQUIPMENT_SLOT_COUNT - 1
					                 ? botConfig.Difficulty
					                 : Constants.EQUIPMENT_SLOT_COUNT - 1;
				
				var botCharacter = new BotCharacter
				{
					Skin = skins[f.RNG->Next(0, skins.Count)],
					BotNameIndex = botNamesIndices[listNamesIndex],
					Weapon = new Equipment(weaponConfig.Id, ItemRarity.Common, 1),
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
				
				for(var j = 0; j < gearPieces; j++)
				{
					var slotIndex = f.RNG->Next(0, gearSlots.Count);
					var slot = gearSlots[slotIndex];
					var slotItems = slot.GetIds();
					var rngGearIndex = f.RNG->Next(-1, slotItems.Count);
					var gearConfig = f.GearConfigs.GetConfig(slotItems[rngGearIndex < 0 ? 0 : rngGearIndex]);
					var equipment = new Equipment(gearConfig.Id, gearConfig.StartingRarity, rngGearIndex < 0 ? 0u : 1u);

					gear[j] = equipment;
					botCharacter.Gear[j] = equipment;
					gearSlots.RemoveAt(slotIndex);
				}
				
				playerSpawners[rngSpawnIndex].Component->ActivationTime = f.Time + Constants.SPAWNER_INACTIVE_TIME;

				if (playerSpawners.Count > 1)
				{
					playerSpawners.RemoveAt(rngSpawnIndex);
				}

				f.Add(botEntity, pathfinder); // Must be defined before the steering agent
				f.Add(botEntity, navMeshAgent);
				f.Add(botEntity, botCharacter);
				
				playerCharacter->Init(f, botEntity, id, spawnerTransform, 1, botCharacter.Skin, botCharacter.Weapon, gear);
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
					entity = !entity.IsValid || f.Get<PlayerSpawner>(entity).ActivationTime > pair.Component->ActivationTime ? pair.Entity : entity;
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