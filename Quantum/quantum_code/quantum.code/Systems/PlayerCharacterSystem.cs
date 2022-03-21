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
	}
}