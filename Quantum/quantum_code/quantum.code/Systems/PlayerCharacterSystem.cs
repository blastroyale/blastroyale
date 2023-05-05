using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="PlayerCharacter"/> and it's dependent component states
	/// </summary>
	public unsafe class PlayerCharacterSystem : SystemMainThreadFilter<PlayerCharacterSystem.PlayerCharacterFilter>, ISignalHealthIsZeroFromAttacker, ISignalAllPlayersJoined
	{
		private static readonly FP TURN_RATE = FP._0_50 + FP._0_05;
		private static readonly FP MOVE_SPEED_UP_CAP = FP._0_50 + FP._0_20 + + FP._0_25;
		private static readonly FP SKYDIVE_FALL_SPEED = -FP._8;
		private static readonly FP SKYDIVE_DIRECTION_MULT = 3;
		
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
			if (f.Context.GameModeConfig.Teams)
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
					membersByTeam["p" + i] = new HashSet<int>() {i};
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
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var playerDead))
			{
				return;
			}

			var deathPosition = f.Get<Transform3D>(entity).Position;
			var step = 0;
			var gameModeConfig = f.Context.GameModeConfig;

			playerDead->Dead(f, entity, attacker, fromRoofDamage);

			// Try to drop player weapon
			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.WeaponOnly && !playerDead->HasMeleeWeapon(f, entity))
			{
				Collectable.DropEquipment(f, playerDead->CurrentWeapon, deathPosition, step);
				step++;
			}

			var itemCount = 0;
			for (int i = 0; i < playerDead->Gear.Length; i++) //loadout items found
			{
				if (playerDead->Gear[i].GameId != GameId.Random)
				{
					itemCount++;
				}
			}

			for (int i = 0; i < playerDead->WeaponSlots.Length; i++) //item slots filled
			{
				if (playerDead->WeaponSlots[i].Weapon.GameId != GameId.Random)
				{
					itemCount++;
				}
			}

			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.Consumables)
			{
				if (!f.Unsafe.TryGetPointer<Stats>(attacker, out var stats) ||
				    !f.Unsafe.TryGetPointer<PlayerCharacter>(attacker, out var attackingPlayer))
				{
					return;
				}

				var ammoFilled = FP.MaxValue;
				var healthFilled = stats->CurrentHealth / stats->GetStatData(StatType.Health).StatValue;
				var shieldFilled = stats->CurrentShield / stats->GetStatData(StatType.Shield).StatValue;

				//drop consumables based on the number of items you have collected and the kind of consumables the player needs
				for (uint i = 0; i < (FPMath.RoundToInt(itemCount / 2) + 1); i++)
				{
					var consumable = GameId.Health;
					if (healthFilled < ammoFilled && healthFilled < shieldFilled) //health
					{
						consumable = GameId.Health;
						healthFilled += f.ConsumableConfigs.GetConfig(consumable).Amount.Get(f) /
							stats->GetStatData(StatType.Health).StatValue;
					}
					else if (ammoFilled < healthFilled && ammoFilled < shieldFilled) //ammo
					{
						consumable = GameId.AmmoSmall;
						ammoFilled += f.ConsumableConfigs.GetConfig(consumable).Amount.Get(f);
					}
					else if (shieldFilled < healthFilled && shieldFilled < ammoFilled) //shield
					{
						consumable = GameId.ShieldSmall;
						shieldFilled += f.ConsumableConfigs.GetConfig(consumable).Amount.Get(f) /
							stats->GetStatData(StatType.Shield).StatValue;
					}

					Collectable.DropConsumable(f, consumable, deathPosition, step, false);
					step++;
				}

				if (QuantumFeatureFlags.DropEnergyCubes)
				{
					Collectable.DropConsumable(f, GameId.EnergyCubeLarge, deathPosition, step, false); //drop a single level on kill

				}
				if (!playerDead->HasMeleeWeapon(f, entity)) //also drop the target player's weapon
				{
					Collectable.DropEquipment(f, playerDead->CurrentWeapon, deathPosition, step);
					step++;
				}
			}

			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.Tutorial)
			{
				Collectable.DropConsumable(f, GameId.Health, deathPosition, step, false);
			}
		}

		private void InstantiatePlayer(Frame f, PlayerRef playerRef, RuntimePlayer playerData, int teamId)
		{
			var playerEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var gridSquareSize = FP._1 * f.Map.WorldSize / f.Map.GridSizeX / FP._2;
			var spawnPosition = playerData.NormalizedSpawnPosition * f.Map.WorldSize +
				new FPVector2(f.RNG->Next(-gridSquareSize, gridSquareSize),
					f.RNG->Next(-gridSquareSize, gridSquareSize));
			var spawnTransform = new Transform3D {Position = FPVector3.Zero, Rotation = FPQuaternion.Identity};

			spawnTransform.Position = spawnPosition.XOY;

			playerCharacter->Init(f, playerEntity, playerRef, spawnTransform, playerData.PlayerLevel,
				playerData.PlayerTrophies, playerData.Skin, playerData.DeathMarker, teamId,
				playerData.Loadout, playerData.Loadout.FirstOrDefault(e => e.IsWeapon()), null, f.Context.GameModeConfig.MinimumHealth);
		}

		private void ProcessPlayerInput(Frame f, ref PlayerCharacterFilter filter)
		{
			// Do not process input if player is stunned or not alive
			if (!f.Has<AlivePlayerCharacter>(filter.Entity) || f.Has<Stun>(filter.Entity) ||
			    f.Has<BotCharacter>(filter.Entity))
			{
				return;
			}

			var input = f.GetPlayerInput(filter.Player->Player);

			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);
			var rotation = FPVector2.Zero;
			var movedirection = FPVector2.Zero;
			var prevRotation = bb->GetVector2(f, Constants.AimDirectionKey);
			var skyDiving = bb->GetBoolean(f, Constants.IsSkydiving);
			var direction = input->Direction;
			var aim = input->AimingDirection;
			var shooting = input->IsShooting;
			var lastShotAt = bb->GetFP(f, Constants.LastShotAt);
			if (direction != FPVector2.Zero || skyDiving) 
			{
				movedirection = direction;
			}
			if(!bb->GetBoolean(f, Constants.IsShootingKey))
			{
				rotation = direction;
			}
			if (aim.SqrMagnitude > FP._0)
			{
				rotation = aim;
			} else if (f.Time < lastShotAt + FP._0_33)
			{
				rotation = prevRotation;
			}
			
			//this way you save your previous attack angle when flicking and only return your movement angle when your shot is finished
			if (rotation == FPVector2.Zero && bb->GetBoolean(f, Constants.IsShootingKey)) 
			{
				rotation = prevRotation;
			}

			var moveSpeed = input->MovementMagnitude;
			if (moveSpeed >= MOVE_SPEED_UP_CAP) moveSpeed = 1;

			var wasShooting = bb->GetBoolean(f, Constants.IsAimPressedKey);
			
			bb->Set(f, Constants.IsAimPressedKey, shooting);
			bb->Set(f, Constants.AimDirectionKey, rotation);
			bb->Set(f, Constants.MoveDirectionKey, movedirection);
			bb->Set(f, Constants.MoveSpeedKey, moveSpeed);
			
			var weaponConfig = f.WeaponConfigs.GetConfig(filter.Player->CurrentWeapon.GameId);
			
			if (!wasShooting && shooting && !weaponConfig.IsMeleeWeapon)
			{
				bb->Set(f, nameof(Constants.NextTapTime), f.Time + weaponConfig.AimDelay);
			}
			
			var aimDirection = bb->GetVector2(f, Constants.AimDirectionKey);
			if (aimDirection.SqrMagnitude > FP._0)
			{
				QuantumHelpers.LookAt2d(f, filter.Entity, aimDirection, f.GameConfig.HardAngleAim ? FP._0 : TURN_RATE );
			}
			
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);
			var maxSpeed = f.GameConfig.PlayerDefaultSpeed.Get(f);
			var moveDirection = bb->GetVector2(f, Constants.MoveDirectionKey).XOY;
			var velocity = kcc->Velocity;

			if (moveSpeed != FP._1)
			{
				maxSpeed *= moveSpeed;
				velocity.X *= moveSpeed;
				velocity.Z *= moveSpeed;
			}

			if (skyDiving)
			{
				maxSpeed *= SKYDIVE_DIRECTION_MULT;
				velocity.Y = SKYDIVE_FALL_SPEED;
			}
			else if(shooting)
			{
				maxSpeed *= weaponConfig.AimingMovementSpeed;
			}
			
			var speedUpMutatorExists = f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
			kcc->MaxSpeed = speedUpMutatorExists?maxSpeed * speedUpMutatorConfig.Param1:maxSpeed;

			kcc->Velocity = velocity;
			
			kcc->Move(f, filter.Entity, moveDirection);
		}

		private void UpdateHealthPerSecMutator(Frame f, ref PlayerCharacterFilter filter)
		{
			if (!f.Context.TryGetMutatorByType(MutatorType.HealthPerSeconds, out var healthPerSecondsMutatorConfig))
			{
				return;
			}

			var health = healthPerSecondsMutatorConfig.Param1.AsInt;
			var seconds = healthPerSecondsMutatorConfig.Param2.AsInt;

			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();

			if (f.Time > gameContainer->MutatorsState.HealthPerSecLastTime + seconds)
			{
				gameContainer->MutatorsState.HealthPerSecLastTime = f.Time;

				if (!f.Unsafe.TryGetPointer<Stats>(filter.Entity, out var stats))
				{
					return;
				}

				var spell = new Spell() {PowerAmount = (uint) health};
				if (health > 0)
				{
					stats->GainHealth(f, filter.Entity, &spell);
				}
				else
				{
					stats->ReduceHealth(f, filter.Entity, & spell);
				}
			}
		}
	}
}