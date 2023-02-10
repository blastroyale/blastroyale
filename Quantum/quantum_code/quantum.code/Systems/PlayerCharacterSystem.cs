using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="PlayerCharacter"/> and it's dependent component states
	/// </summary>
	public unsafe class PlayerCharacterSystem : SystemMainThreadFilter<PlayerCharacterSystem.PlayerCharacterFilter>,
												ISignalHealthIsZeroFromAttacker, ISignalAllPlayersJoined
	{
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
			var parties = new Dictionary<string, int>();

			int partyIndex = Constants.TEAM_ID_START_PARTIES;

			for (var i = 0; i < f.PlayerCount; i++)
			{
				var playerData = f.GetPlayerData(i);

				if (playerData != null &&
					!string.IsNullOrEmpty(playerData.PartyId) &&
					!parties.ContainsKey(playerData.PartyId))
				{
					parties.Add(playerData.PartyId, partyIndex++);
				}
			}

			for (var i = 0; i < f.PlayerCount; i++)
			{
				var playerData = f.GetPlayerData(i);

				if (playerData == null) continue;

				var teamId = !string.IsNullOrEmpty(playerData.PartyId)
					? parties[playerData.PartyId]
					: Constants.TEAM_ID_START_PLAYERS + i;

				InstantiatePlayer(f, i, playerData, teamId);
			}
			
			f.Signals.AllPlayersSpawned();
		}

		/// <inheritdoc />
		public void HealthIsZeroFromAttacker(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var playerDead))
			{
				return;
			}

			var deathPosition = f.Get<Transform3D>(entity).Position;
			var step = 0;
			var gameModeConfig = f.Context.GameModeConfig;

			playerDead->Dead(f, entity, attacker);

			// Try to drop player weapon
			if ((gameModeConfig.DeathDropStrategy == DeathDropsStrategy.WeaponOnly ||
					gameModeConfig.DeathDropStrategy == DeathDropsStrategy.BoxAndWeapon) &&
				!playerDead->HasMeleeWeapon(f, entity))
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

			//drop a chest based on how many items the player has collected
			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.Box ||
				gameModeConfig.DeathDropStrategy == DeathDropsStrategy.BoxAndWeapon)
			{
				//drop a box based on the number of items the player has collected
				var dropBox = GameId.ChestCommon;

				// Calculate offset position to drop a box so it tries not to cover the death marker
				var dropOffset = FPVector2.Rotate(FPVector2.Left * Constants.DROP_OFFSET_RADIUS,
					f.RNG->Next(0, FP.Rad_180 * 2)).XOY;
				var dropPosition = deathPosition + dropOffset;

				QuantumHelpers.TryFindPosOnNavMesh(f, dropPosition, Constants.DROP_OFFSET_RADIUS * FP._0_50,
					out dropPosition);

				dropBox = f.ChestConfigs.CheckItemRange(itemCount);
				CollectablePlatformSpawner.SpawnChest(f, dropBox, dropPosition, entity);

			}

			if (gameModeConfig.DeathDropStrategy == DeathDropsStrategy.Consumables)
			{
				if (!f.Unsafe.TryGetPointer<Stats>(attacker, out var stats) ||
					!f.Unsafe.TryGetPointer<PlayerCharacter>(attacker, out var attackingPlayer))
				{
					return;
				}

				var ammoFilled = attackingPlayer->GetAmmoAmountFilled(f, attacker);
				var healthFilled = stats->CurrentHealth / stats->GetStatData(StatType.Health).StatValue;
				var shieldFilled = stats->CurrentShield / stats->GetStatData(StatType.Shield).StatValue;

				//drop consumables based on the number of items you have collected and the kind of consumables the player needs
				for (int i = 0; i < FPMath.RoundToInt(itemCount / 2); i++)
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
				playerData.Loadout, playerData.Loadout.FirstOrDefault(e => e.IsWeapon()));
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

			var direction = input->Direction;
			var aim = input->AimingDirection;
			var shooting = input->IsShooting;

			if (direction != FPVector2.Zero) 
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
			}
			//this way you save your previous attack angle when flicking and only return your movement angle when your shot is finished
			if (rotation == FPVector2.Zero && bb->GetBoolean(f, Constants.IsShootingKey)) 
			{
				rotation = prevRotation;
			}

			bb->Set(f, Constants.IsAimPressedKey, shooting);
			bb->Set(f, Constants.AimDirectionKey, rotation);
			bb->Set(f, Constants.MoveDirectionKey, movedirection);
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

				if (health > 0)
				{
					stats->GainHealth(f, filter.Entity, new Spell() {PowerAmount = (uint) health});
				}
				else
				{
					stats->ReduceHealth(f, filter.Entity, new Spell() {PowerAmount = (uint) health});
				}
			}
		}
	}
}