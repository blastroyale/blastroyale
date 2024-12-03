using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum.Core;
using Quantum.Physics2D;
using Quantum.Physics3D;
using Quantum.Profiling;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="PlayerCharacter"/> and it's dependent component states
	/// </summary>
	public unsafe class PlayerCharacterSystem : SystemMainThreadFilter<PlayerCharacterSystem.PlayerCharacterFilter>,
												ISignalHealthIsZeroFromAttacker,
												ISignalAllPlayersJoined
	{
		private static readonly FP TURN_RATE = FP._0_50 + FP._0_05;
		private static readonly FP MOVE_SPEED_UP_CAP = FP._0_50 + FP._0_20 + FP._0_25;

		public struct PlayerCharacterFilter
		{
			public EntityRef Entity;
			public PlayerCharacter* Player;
			public AlivePlayerCharacter* Alive;
			public AIBlackboardComponent* Ai;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref PlayerCharacterFilter filter)
		{
			ProcessPlayerInput(f, ref filter);
			UpdateHealthPerSecMutator(f, ref filter);
		}

		/// <inheritdoc />
		public void AllPlayersJoined(Frame f)
		{
			Dictionary<int, int> teamsByPlayer;
			if (f.GetTeamSize() > 1)
			{
				teamsByPlayer = GeneratePlayerTeamIds(f);
			}
			else
			{
				teamsByPlayer = new Dictionary<int, int>();
			}

			for (var i = 0; i < f.PlayerCount; i++)
			{
				var playerData = f.GetPlayerData(i);

				if (playerData == null) continue;

				var teamId = teamsByPlayer.ContainsKey(i)
					? teamsByPlayer[i]
					: Constants.TEAM_ID_START_PLAYERS + i;

				InstantiateRealPlayer(f, i, playerData, teamId);
			}

			f.Signals.AllPlayersSpawned(); // Bots are added here
			DeleteSpawners(f);
		}

		private void DeleteSpawners(Frame f)
		{
			foreach (var entityComponentPointerPair in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				f.Destroy(entityComponentPointerPair.Entity);
			}
		}

		/// <summary>
		/// Returns a dictionary containing PLAYER_ID:TEAM_ID
		/// </summary>
		/// <param name="f"></param>
		/// <returns></returns>
		private Dictionary<int, int> GeneratePlayerTeamIds(Frame f)
		{
			var membersByTeam = new Dictionary<string, HashSet<int>>();

			for (var i = 0; i < f.PlayerCount; i++)
			{
				var playerData = f.GetPlayerData(i);

				if (playerData != null &&
					!string.IsNullOrEmpty(playerData.PartyId))
				{
					if (!membersByTeam.TryGetValue(playerData.PartyId, out var data))
					{
						data = new HashSet<int>();
						membersByTeam[playerData.PartyId] = data;
					}

					data.Add(i);
				}
				else
				{
					membersByTeam["p" + i] = new HashSet<int>() { i };
				}
			}

			int partyIndex = Constants.TEAM_ID_START_PARTIES;
			var teamByPlayer = new Dictionary<int, int>();
			foreach (var kv in membersByTeam)
			{
				foreach (var i in kv.Value)
				{
					teamByPlayer[i] = partyIndex;
				}

				partyIndex++;
			}

			return teamByPlayer;
		}

		/// <inheritdoc />
		public void HealthIsZeroFromAttacker(Frame f, EntityRef entity, EntityRef attacker, QBoolean fromRoofDamage)
		{
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var playerDead) ||
				!f.Unsafe.TryGetPointer<Stats>(entity, out var deadStats))
			{
				return;
			}


			var deathPosition = f.Unsafe.GetPointer<Transform2D>(entity)->Position;
			var gameModeConfig = f.Context.GameModeConfig;
			var equipmentToDrop = new List<Equipment>();
			var consumablesToDrop = new List<GameId>();

			playerDead->Dead(f, entity, attacker, fromRoofDamage);

			if (f.Has<AfkPlayer>(entity))
			{
				return;
			}

			// Don't drop items for last player dead
			if (f.Unsafe.GetPointerSingleton<GameContainer>()->IsGameGoingToEndWithKill(f, entity))
			{
				return;
			}

			// Bots dying in no match bot don't drop anything, this is a hack,becase bots needed to be created toa void fucking the whole game state
			if (f.RuntimeConfig.MatchConfigs.DisableBots && f.Has<BotCharacter>(entity))
			{
				return;
			}

			// Try to drop player weapon
			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.WeaponOnly &&
				!PlayerCharacter.HasMeleeWeapon(f, entity))
			{
				equipmentToDrop.Add(playerDead->CurrentWeapon);
			}

			// We drop two items. One is always a consumable. Another can be a gun (50% chance) or consumable
			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.Consumables)
			{
				var consumable = QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall, GameId.Health, GameId.ShieldSmall);
				consumablesToDrop.Add(consumable);

				if (playerDead->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon.IsValid()
					&& f.RNG->Next(FP._0, FP._1) <
					Constants.CHANCE_TO_DROP_WEAPON_ON_DEATH) //also drop the target player's weapon
				{
					equipmentToDrop.Add(playerDead->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon);
				}
				else
				{
					// Avoid dropping the same consumable from a single player twice
					switch (consumable)
					{
						case GameId.AmmoSmall:
							consumablesToDrop.Add(QuantumHelpers.GetRandomItem(f, GameId.Health, GameId.ShieldSmall));
							break;
						case GameId.Health:
							consumablesToDrop.Add(QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall,
								GameId.ShieldSmall));
							break;
						case GameId.ShieldSmall:
							consumablesToDrop.Add(QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall, GameId.Health));
							break;
					}
				}

				foreach (var metaItemDropOverwrite in f.RuntimeConfig.MatchConfigs.MetaItemDropOverwrites
							 .Where(d => d.Place == DropPlace.Player))
				{
					var rnd = f.RNG->Next();
					if (rnd <= metaItemDropOverwrite.DropRate)
					{
						var amount = f.RNG->Next(metaItemDropOverwrite.MinDropAmount,
							metaItemDropOverwrite.MaxDropAmount);
						for (var i = 0; i < amount; i++)
						{
							consumablesToDrop.Add(metaItemDropOverwrite.Id);
						}
					}
				}
			}

			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.Tutorial)
			{
				// No need to drop anything from killed dummies
				// they don't even shoot anymore (first ones)
			}

			var anglesToDrop = equipmentToDrop.Count + consumablesToDrop.Count;
			var step = 0;
			foreach (var drop in equipmentToDrop)
			{
				Collectable.DropEquipment(f, drop, deathPosition, step, true, anglesToDrop);
				step++;
			}

			var noHealthNoShields = f.Context.Mutators.HasFlagFast(Mutator.Hardcore);

			foreach (var drop in consumablesToDrop)
			{
				if (noHealthNoShields &&
					(drop == GameId.Health ||
						drop == GameId.ShieldSmall))
				{
					// Don't drop Health and Shields with Hardcore mutator
				}
				else
				{
					Collectable.DropConsumable(f, drop, deathPosition, step, true, anglesToDrop);
				}

				step++;
			}
		}

		private void InstantiateRealPlayer(Frame f, PlayerRef playerRef, RuntimePlayer playerData, int teamId)
		{
			var playerEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			playerCharacter->RealPlayer = true;
			var gridSquareSize = FP._1 * f.Map.WorldSize / f.Map.GridSizeX / FP._2;
			var spawnPosition = playerData.NormalizedSpawnPosition * f.Map.WorldSize +
				new FPVector2(f.RNG->Next(-gridSquareSize, gridSquareSize),
					f.RNG->Next(-gridSquareSize, gridSquareSize));
			var spawner = QuantumHelpers.GetPlayerSpawnPosition(f, spawnPosition);
			var spawnTransform = new Transform2D
				{ Position = spawner.Component->Position, Rotation = spawner.Component->Rotation };
			var setup = new PlayerCharacterSetup()
			{
				e = playerEntity,
				playerRef = playerRef,
				spawnPosition = spawnTransform,
				playerLevel = playerData.PlayerLevel,
				trophies = playerData.PlayerTrophies,
				teamId = teamId,
				modifiers = null,
				minimumHealth = f.Context.GameModeConfig.MinimumHealth,
			};
			// Skin stuff
			f.Add<CosmeticsHolder>(playerEntity);
			f.Unsafe.GetPointer<CosmeticsHolder>(playerEntity)->SetCosmetics(f, playerData.Cosmetics);
			playerCharacter->Init(f, setup);

			// Remove used spawner
			f.Destroy(spawner.Entity);
		}

		/// <summary>
		/// When player starts to aim, there is an initial delay for when a bullet needs to be fired.
		/// </summary>
		public static void OnStartAiming(Frame f, AIBlackboardComponent* bb, in QuantumWeaponConfig weaponConfig)
		{
			if (weaponConfig.IsMeleeWeapon) return; // melee weapons are instant
			var nextShotTime = bb->GetFP(f, Constants.NEXT_SHOT_TIME);
			var expectedAimDelayShot = f.Time + weaponConfig.AimDelayTime;
			var isInCooldown = nextShotTime > f.Time;
			// If the shoot cooldown will finish after the aim delay, we use it instead
			if (isInCooldown && nextShotTime > expectedAimDelayShot) expectedAimDelayShot = nextShotTime;
			bb->Set(f, Constants.NEXT_SHOT_TIME, expectedAimDelayShot);
			bb->Set(f, Constants.NEXT_TAP_TIME, expectedAimDelayShot);
		}

		private void ProcessPlayerInput(Frame f, ref PlayerCharacterFilter filter)
		{
			// Do not process input if player is stunned or not alive
			if (f.Has<Stun>(filter.Entity) || f.Has<BotCharacter>(filter.Entity))
			{
				return;
			}

			var bb = filter.Ai;
			// Do nothing when Skydiving as it handled via animation
			if (bb->GetBoolean(f, Constants.IS_SKYDIVING))
			{
				return;
			}

			var input = f.GetPlayerInput(filter.Player->Player);

			// Check inactivity only up to certain time and only in ranked matches
			if (f.Time > f.GameConfig.NoInputStartChecking &&
				f.Time < f.GameConfig.NoInputStopChecking &&
				f.RuntimeConfig.MatchConfigs.MatchType == MatchType.Matchmaking)
			{
				ProcessNoInputWarning(f, ref filter, input->GetHashCode());
			}

			var targetAimDirection = FPVector2.Zero;
			var movedirection = FPVector2.Zero;
			var prevAimDirection = bb->GetVector2(f, Constants.AIM_DIRECTION_KEY);

			var isKnockedOut = ReviveSystem.IsKnockedOut(f, filter.Entity);
			var inputDirection = input->Direction;
			var inputAim = input->AimingDirection;
			var shooting = input->IsShooting && !isKnockedOut;
			var lastShotAt = bb->GetFP(f, Constants.LAST_SHOT_AT);
			var weaponConfig = f.WeaponConfigs.GetConfig(filter.Player->CurrentWeapon.GameId);
			var attackCooldown = f.Time < lastShotAt + (weaponConfig.IsMeleeWeapon ? FP._0_33 : FP._0_20);

			if (inputDirection != FPVector2.Zero)
			{
				movedirection = inputDirection;
			}

			if (!bb->GetBoolean(f, Constants.IS_SHOOTING_KEY))
			{
				targetAimDirection = inputDirection;
			}

			if (inputAim.SqrMagnitude > FP._0)
			{
				targetAimDirection = inputAim;
			}
			else if (attackCooldown)
			{
				targetAimDirection = prevAimDirection;
			}

			//this way you save your previous attack angle when flicking and only return your movement angle when your shot is finished
			if (targetAimDirection == FPVector2.Zero && bb->GetBoolean(f, Constants.IS_SHOOTING_KEY))
			{
				targetAimDirection = prevAimDirection;
			}

			if (isKnockedOut)
			{
				targetAimDirection = inputDirection;
			}

			var wasShooting = bb->GetBoolean(f, Constants.IS_AIM_PRESSED_KEY);
			if (!wasShooting && shooting)
			{
				OnStartAiming(f, bb, weaponConfig);
			}

			bb->Set(f, Constants.IS_AIM_PRESSED_KEY, shooting);
			bb->Set(f, Constants.AIM_DIRECTION_KEY, targetAimDirection);
			bb->Set(f, Constants.MOVE_SPEED_KEY, 1);

			var kcc = f.Unsafe.GetPointer<TopDownController>(filter.Entity);
			var maxSpeed = f.Unsafe.GetPointer<Stats>(filter.Entity)->GetStatData(StatType.Speed).StatValue;

			if (shooting)
			{
				maxSpeed *= weaponConfig.AimingMovementSpeed;
			}

			ReviveSystem.OverwriteMaxMoveSpeed(f, filter.Entity, ref maxSpeed);

			var speedUpMutatorExists = f.Context.Mutators.HasFlagFast(Mutator.SpeedUp);
			kcc->MaxSpeed = speedUpMutatorExists ? maxSpeed * Constants.MUTATOR_SPEEDUP_AMOUNT : maxSpeed;
			kcc->AimDirection = targetAimDirection;
			kcc->MoveDirection = movedirection;
		}

		private void ProcessNoInputWarning(Frame f, ref PlayerCharacterFilter filter, int inputHashCode)
		{
			if (filter.Player->InputSnapshot == inputHashCode)
			{
				if (filter.Player->IsAfk(f))
				{
					f.Add<AfkPlayer>(filter.Entity);
					f.Signals.PlayerKilledByBeingAFK(filter.Player->Player);
					f.Unsafe.GetPointer<Stats>(filter.Entity)->Kill(f, filter.Entity);
				}
				else if (f.Time - filter.Player->LastNoInputTimeSnapshot > f.GameConfig.NoInputWarningTime
						 && f.Time - filter.Player->LastNoInputTimeSnapshot < f.GameConfig.NoInputWarningTime + FP._1)
				{
					f.Events.OnLocalPlayerNoInput(f.Unsafe.GetPointer<PlayerCharacter>(filter.Entity)->Player,
						filter.Entity);

					// A hack with a time counter to avoid sending more than a single event
					filter.Player->LastNoInputTimeSnapshot -= FP._1_50;
				}
			}
			else
			{
				filter.Player->LastNoInputTimeSnapshot = f.Time;
			}

			filter.Player->InputSnapshot = inputHashCode;
		}

		private void UpdateHealthPerSecMutator(Frame f, ref PlayerCharacterFilter filter)
		{
			if (!f.IsVerified) return;
			if (!f.Context.Mutators.HasFlagFast(Mutator.HealthyAir))
			{
				return;
			}

			var seconds = Constants.MUTATOR_HEALTHPERSECONDS_DURATION;

			// It will heal every x frames
			var frames = seconds * f.UpdateRate;
			if (f.Number % frames != 0) return;

			if (!f.Unsafe.TryGetPointer<Stats>(filter.Entity, out var stats))
			{
				return;
			}

			var health = Constants.MUTATOR_HEALTHPERSECONDS_AMOUNT;

			var spell = new Spell() { PowerAmount = (uint)health };
			if (health > 0)
			{
				stats->GainHealth(f, filter.Entity, &spell);
			}
			else
			{
				stats->ReduceHealth(f, filter.Entity, &spell);
			}
		}

		public bool OnCharacterCollision2D(Frame f, EntityRef character, Hit hit)
		{
			var blockMovement = !QuantumFeatureFlags.TEAM_IGNORE_COLLISION ||
				!TeamSystem.HasSameTeam(f, character, hit.Entity);
			if (!QuantumFeatureFlags.PLAYER_PUSHING) return blockMovement;
			if (blockMovement && f.TryGet<TopDownController>(hit.Entity, out var enemyKcc) &&
				f.TryGet<TopDownController>(character, out var myKcc))
			{
				var myTransform = f.Unsafe.GetPointer<Transform2D>(character);
				var enemyTransform = f.Unsafe.GetPointer<Transform2D>(hit.Entity);
				var pushAngle = (myTransform->Position - enemyTransform->Position).Normalized;
				pushAngle.Y = 0;
				enemyKcc.Move(f, hit.Entity, pushAngle);
			}

			return blockMovement;
		}

		public void OnCharacterTrigger2D(FrameBase frame, EntityRef character, Hit hit)
		{
		}
	}
}