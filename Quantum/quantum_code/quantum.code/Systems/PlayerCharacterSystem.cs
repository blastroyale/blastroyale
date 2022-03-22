using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="PlayerCharacter"/> and it's dependent component states
	/// </summary>
	public unsafe class PlayerCharacterSystem : SystemMainThreadFilter<PlayerCharacterSystem.PlayerCharacterFilter>,
	                                            ISignalOnPlayerDataSet, ISignalPlayerKilledPlayer, ISignalHealthIsZero
	{
		public struct PlayerCharacterFilter
		{
			public EntityRef Entity;
			public PlayerCharacter* Player;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref PlayerCharacterFilter filter)
		{
			ProcessPlayerDisconnect(f, ref filter);
			ProcessPlayerInput(f, ref filter);
		}

		/// <inheritdoc />
		public void OnPlayerDataSet(Frame f, PlayerRef playerRef)
		{
			var playerData = f.GetPlayerData(playerRef);
			var spawnPosition = playerData.NormalizedSpawnPosition * f.Map.WorldSize;
			var playerEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var spawnTransform = new Transform3D {Position = FPVector3.Zero, Rotation = FPQuaternion.Identity};

			// TODO: Move this to Spawn Action
			QuantumHelpers.TryFindPosOnNavMesh(f, spawnPosition.XOY, out var closestPosition);

			spawnTransform.Position = closestPosition;

			playerCharacter->Init(f, playerEntity, playerRef, spawnTransform, playerData.PlayerLevel,
			                      playerData.Skin, playerData.Weapon, playerData.Gear);
		}

		/// <inheritdoc />
		public void HealthIsZero(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var player))
			{
				return;
			}

			if (f.TryGet<PlayerCharacter>(attacker, out var killer))
			{
				f.Signals.PlayerKilledPlayer(player->Player, entity, killer.Player, attacker);
				f.Events.OnPlayerKilledPlayer(player->Player, killer.Player);
			}

			player->Dead(f, entity, killer.Player, attacker);
		}

		/// <inheritdoc />
		public void PlayerKilledPlayer(Frame f, PlayerRef playerDead, EntityRef entityDead, PlayerRef playerKiller,
		                               EntityRef entityKiller)
		{
			var deathPosition = f.Get<Transform3D>(entityDead).Position;
			var armourDropChance = f.RNG->Next();
			var step = 0;

			// Try to drop Health pack
			if (f.RNG->Next() <= f.GameConfig.DeathDropHealthChance)
			{
				Collectable.DropCollectable(f, GameId.Health, deathPosition, step, false);

				step++;
			}

			// Try to drop InterimArmourLarge, if didn't work then try to drop InterimArmourSmall
			if (armourDropChance <= f.GameConfig.DeathDropInterimArmourLargeChance)
			{
				Collectable.DropCollectable(f, GameId.InterimArmourLarge, deathPosition, step, false);

				step++;
			}
			else if (armourDropChance <= f.GameConfig.DeathDropInterimArmourSmallChance +
			         f.GameConfig.DeathDropInterimArmourLargeChance)
			{
				Collectable.DropCollectable(f, GameId.InterimArmourSmall, deathPosition, step, false);

				step++;
			}

			// Try to drop Weapon (if it's not Melee)
			if (!f.Get<PlayerCharacter>(entityDead).HasMeleeWeapon(f, entityDead) &&
			    f.RNG->Next() <= f.GameConfig.DeathDropWeaponChance)
			{
				Collectable.DropCollectable(f, f.Get<PlayerCharacter>(entityDead).CurrentWeapon.GameId, deathPosition,
				                            step, true);
			}
		}

		private void ProcessPlayerDisconnect(Frame f, ref PlayerCharacterFilter filter)
		{
			if (f.Has<BotCharacter>(filter.Entity))
			{
				return;
			}

			if ((f.GetPlayerInputFlags(filter.Player->Player) & DeterministicInputFlags.PlayerNotPresent) == 0)
			{
				filter.Player->DisconnectedDuration = 0;

				return;
			}

			filter.Player->DisconnectedDuration += f.DeltaTime;

			if (filter.Player->DisconnectedDuration > f.GameConfig.DisconnectedDestroySeconds)
			{
				filter.Player->PlayerLeft(f, filter.Entity);
			}
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
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);
			var rotation = FPVector2.Zero;
			var moveVelocity = FPVector3.Zero;
			var bb = f.Get<AIBlackboardComponent>(filter.Entity);
			var weaponConfig = f.WeaponConfigs.GetConfig(filter.Player->CurrentWeapon.GameId);

			if (input->IsMoveButtonDown)
			{
				var speed = f.Get<Stats>(filter.Entity).Values[(int) StatType.Speed].StatValue;

				if (input->IsShootButtonDown)
				{
					speed *= weaponConfig.AimingMovementSpeed;
				}

				rotation = input->Direction;
				kcc->MaxSpeed = speed;
				moveVelocity = rotation.XOY * speed;
			}

			if (input->AimingDirection.SqrMagnitude > FP._0)
			{
				rotation = input->AimingDirection;
			}

			bb.Set(f, Constants.IsAimingKey, input->IsShootButtonDown);
			bb.Set(f, Constants.AimDirectionKey, rotation);
			bb.Set(f, Constants.MoveDirectionKey, moveVelocity);
		}
	}
}