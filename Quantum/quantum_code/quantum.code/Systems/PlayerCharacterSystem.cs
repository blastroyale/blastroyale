using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum.Core;
using Quantum.Physics3D;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="PlayerCharacter"/> and it's dependent component states
	/// </summary>
	public unsafe class PlayerCharacterSystem : SystemMainThreadFilter<PlayerCharacterSystem.PlayerCharacterFilter>,
												IKCCCallbacks3D, ISignalHealthIsZeroFromAttacker,
												ISignalAllPlayersJoined
	{
		private static readonly FP TURN_RATE = FP._0_50 + FP._0_05;
		private static readonly FP MOVE_SPEED_UP_CAP = FP._0_50 + FP._0_20 + FP._0_25;
		public static readonly FP AIM_DELAY = FP._0_50;

		public struct PlayerCharacterFilter
		{
			public EntityRef Entity;
			public PlayerCharacter* Player;
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

				InstantiatePlayer(f, i, playerData, teamId);
			}

			f.Signals.AllPlayersSpawned();
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
			
			
			var deathPosition = f.Get<Transform3D>(entity).Position;
			var gameModeConfig = f.Context.GameModeConfig;
			var equipmentToDrop = new List<Equipment>();
			var consumablesToDrop = new List<GameId>();

			playerDead->Dead(f, entity, attacker, fromRoofDamage);

			// Try to drop player weapon
			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.WeaponOnly &&
				!playerDead->HasMeleeWeapon(f, entity))
			{
				equipmentToDrop.Add(playerDead->CurrentWeapon);
			}

			// We drop two items. One is always a consumable. Another can be a gun (50% chance) or consumable
			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.Consumables)
			{
				var consumable = QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall, GameId.Health, GameId.ShieldSmall);
				consumablesToDrop.Add(consumable);

				if (playerDead->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon.IsValid()
					&& f.RNG->Next(FP._0, FP._1) < Constants.CHANCE_TO_DROP_WEAPON_ON_DEATH) //also drop the target player's weapon
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
							consumablesToDrop.Add(QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall, GameId.ShieldSmall));
							break;
						case GameId.ShieldSmall:
							consumablesToDrop.Add(QuantumHelpers.GetRandomItem(f, GameId.AmmoSmall, GameId.Health));
							break;
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
			
			var noHealthNoShields = f.Context.TryGetMutatorByType(MutatorType.Hardcore, out _);

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

		private void InstantiatePlayer(Frame f, PlayerRef playerRef, RuntimePlayer playerData, int teamId)
		{
			var playerEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			playerCharacter->RealPlayer = true;
			var gridSquareSize = FP._1 * f.Map.WorldSize / f.Map.GridSizeX / FP._2;
			var spawnPosition = playerData.NormalizedSpawnPosition * f.Map.WorldSize +
				new FPVector2(f.RNG->Next(-gridSquareSize, gridSquareSize),
					f.RNG->Next(-gridSquareSize, gridSquareSize));
			var spawnTransform = new Transform3D { Position = FPVector3.Zero, Rotation = FPQuaternion.Identity };
			spawnTransform.Position = spawnPosition.XOY;
			var kccConfig = f.FindAsset<CharacterController3DConfig>(playerCharacter->KccConfigRef.Id);
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
				KccConfig = kccConfig,
				deathFlagID = playerData.DeathFlagID
			};
			// Skin stuff
			f.Add<CosmeticsHolder>(playerEntity);
			f.Unsafe.GetPointer<CosmeticsHolder>(playerEntity)->SetCosmetics(f, playerData.Cosmetics);
			playerCharacter->Init(f, setup);
		}

		/// <summary>
		/// When player starts to aim, there is an initial delay for when a bullet needs to be fired.
		/// </summary>
		public static void OnStartAiming(Frame f, AIBlackboardComponent* bb, QuantumWeaponConfig weaponConfig)
		{
			if (weaponConfig.IsMeleeWeapon) return; // melee weapons are instant
			var nextShotTime = bb->GetFP(f, nameof(Constants.NextShotTime));
			var expectedAimDelayShot = f.Time + AIM_DELAY;
			var isInCooldown = nextShotTime > f.Time;
			// If the shoot cooldown will finish after the aim delay, we use it instead
			if (isInCooldown && nextShotTime > expectedAimDelayShot) expectedAimDelayShot = nextShotTime;
			bb->Set(f, nameof(Constants.NextShotTime), expectedAimDelayShot);
			bb->Set(f, nameof(Constants.NextTapTime), expectedAimDelayShot);
		}

		private void ProcessPlayerInput(Frame f, ref PlayerCharacterFilter filter)
		{
			// Do not process input if player is stunned or not alive
			if (!f.Has<AlivePlayerCharacter>(filter.Entity) || f.Has<Stun>(filter.Entity) ||
				f.Has<BotCharacter>(filter.Entity))
			{
				return;
			}

			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);
			// Do nothing when Skydiving as it handled via animation
			if (bb->GetBoolean(f, Constants.IsSkydiving))
			{
				return;
			}

			var input = f.GetPlayerInput(filter.Player->Player);
			var rotation = FPVector2.Zero;
			var movedirection = FPVector2.Zero;
			var prevRotation = bb->GetVector2(f, Constants.AimDirectionKey);

			var isKnockedOut = ReviveSystem.IsKnockedOut(f, filter.Entity);
			var direction = input->Direction;
			var aim = input->AimingDirection;
			var shooting = input->IsShooting && !isKnockedOut;
			var lastShotAt = bb->GetFP(f, Constants.LastShotAt);
			var weaponConfig = f.WeaponConfigs.GetConfig(filter.Player->CurrentWeapon.GameId);
			var attackCooldown = f.Time < lastShotAt + (weaponConfig.IsMeleeWeapon ? FP._0_33 : FP._0_20);
			if (direction != FPVector2.Zero)
			{
				movedirection = direction;
			}

			if (!bb->GetBoolean(f, Constants.IsShootingKey))
			{
				rotation = direction;
			}

			if (aim.SqrMagnitude > FP._0)
			{
				rotation = aim;
			}
			else if (attackCooldown)
			{
				rotation = prevRotation;
			}

			//this way you save your previous attack angle when flicking and only return your movement angle when your shot is finished
			if (rotation == FPVector2.Zero && bb->GetBoolean(f, Constants.IsShootingKey))
			{
				rotation = prevRotation;
			}

			var wasShooting = bb->GetBoolean(f, Constants.IsAimPressedKey);

			bb->Set(f, Constants.IsAimPressedKey, shooting);
			bb->Set(f, Constants.AimDirectionKey, rotation);
			bb->Set(f, Constants.MoveDirectionKey, movedirection);
			bb->Set(f, Constants.MoveSpeedKey, 1);

			if (!wasShooting && shooting)
			{
				OnStartAiming(f, bb, weaponConfig);
			}

			var aimDirection = bb->GetVector2(f, Constants.AimDirectionKey);
			if (aimDirection.SqrMagnitude > FP._0)
			{
				QuantumHelpers.LookAt2d(f, filter.Entity, aimDirection, f.GameConfig.HardAngleAim ? FP._0 : TURN_RATE);
			}

			var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);
			var maxSpeed = f.Unsafe.GetPointer<Stats>(filter.Entity)->GetStatData(StatType.Speed).StatValue;
			var moveDirection = bb->GetVector2(f, Constants.MoveDirectionKey).XOY;
			var velocity = kcc->Velocity;

			if (shooting)
			{
				maxSpeed *= weaponConfig.AimingMovementSpeed;
			}

			ReviveSystem.OverwriteMaxMoveSpeed(f, filter.Entity, ref maxSpeed);

			var speedUpMutatorExists = f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
			kcc->MaxSpeed = speedUpMutatorExists ? maxSpeed * speedUpMutatorConfig.Param1 : maxSpeed;

			kcc->Velocity = velocity;

			kcc->Move(f, filter.Entity, moveDirection, this);
		}

		private void UpdateHealthPerSecMutator(Frame f, ref PlayerCharacterFilter filter)
		{
			if (!f.IsVerified) return;
			if (!f.Context.TryGetMutatorByType(MutatorType.HealthPerSeconds, out var healthPerSecondsMutatorConfig))
			{
				return;
			}

			var health = healthPerSecondsMutatorConfig.Param1.AsInt;
			var seconds = healthPerSecondsMutatorConfig.Param2.AsInt;

			// It will heal every x frames
			var frames = seconds * f.UpdateRate;
			if (f.Number % frames != 0) return;
			
			if (!f.Unsafe.TryGetPointer<Stats>(filter.Entity, out var stats))
			{
				return;
			}

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

		public bool OnCharacterCollision3D(FrameBase f, EntityRef character, Hit3D hit)
		{
			var blockMovement = !QuantumFeatureFlags.TEAM_IGNORE_COLLISION || !TeamSystem.HasSameTeam(f, character, hit.Entity);
			if (!QuantumFeatureFlags.PLAYER_PUSHING) return blockMovement;
			if (blockMovement && f.TryGet<CharacterController3D>(hit.Entity, out var enemyKcc) &&
				f.TryGet<CharacterController3D>(character, out var myKcc))
			{
				var myTransform = f.Get<Transform3D>(character);
				var enemyTransform = f.Unsafe.GetPointer<Transform3D>(hit.Entity);
				var pushAngle = (myTransform.Position - enemyTransform->Position).Normalized;
				pushAngle.Y = 0;
				enemyKcc.Move(f, hit.Entity, pushAngle);
			}

			return blockMovement;
		}

		public void OnCharacterTrigger3D(FrameBase f, EntityRef character, Hit3D hit)
		{
		}
	}
}