using System;
using System.Linq;
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
			ProcessPlayerInput(f, ref filter);
		}

		/// <inheritdoc />
		public void OnPlayerDataSet(Frame f, PlayerRef playerRef)
		{
			var playerData = f.GetPlayerData(playerRef);

			var gridSquareSize = FP._1 * f.Map.WorldSize / f.Map.GridSizeX / FP._2;
			var spawnPosition = playerData.NormalizedSpawnPosition * f.Map.WorldSize +
			                    new FPVector2(f.RNG->Next(-gridSquareSize, gridSquareSize),
			                                  f.RNG->Next(-gridSquareSize, gridSquareSize));

			var playerEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var spawnTransform = new Transform3D {Position = FPVector3.Zero, Rotation = FPQuaternion.Identity};

			spawnTransform.Position = spawnPosition.XOY;

			var startingEquipment = f.Context.MapConfig.GameMode == GameMode.BattleRoyale
				                        ? Array.Empty<Equipment>()
				                        : playerData.Loadout;

			playerCharacter->Init(f, playerEntity, playerRef, spawnTransform, playerData.PlayerLevel,
			                      playerData.PlayerTrophies, playerData.Skin, startingEquipment,
			                      playerData.Loadout.FirstOrDefault(e => e.IsWeapon()));
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
			var gameMode = f.Context.MapConfig.GameMode;

			//when you kill a player in BR we drop also his/hers weapon
			if (f.Context.MapConfig.GameMode == GameMode.BattleRoyale &&
			    !f.Get<PlayerCharacter>(entityDead).HasMeleeWeapon(f, entityDead))
			{
				Collectable.DropEquipment(f, f.Get<PlayerCharacter>(entityDead).CurrentWeapon, deathPosition, step);
				step++;
			}

			// Try to drop Health pack
			if (f.RNG->Next() <= f.GameConfig.DeathDropHealthChance)
			{
				Collectable.DropConsumable(f, GameId.Health, deathPosition, step, false);
				step++;
			}
			else if (gameMode == GameMode.BattleRoyale)
			{
				Collectable.DropConsumable(f, GameId.AmmoSmall, deathPosition, step, false);
				step++;
			}

			// Try to drop ShieldLarge
			if (armourDropChance <= f.GameConfig.DeathDropLargeShieldChance)
			{
				Collectable.DropConsumable(f, GameId.ShieldLarge, deathPosition, step, false);
			}
			else if (gameMode == GameMode.BattleRoyale && armourDropChance <= f.GameConfig.DeathDropSmallShieldChance)
			{
				Collectable.DropConsumable(f, GameId.ShieldSmall, deathPosition, step, false);
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
			var rotation = FPVector2.Zero;
			var movedirection = FPVector2.Zero;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);

			if (input->IsMoveButtonDown)
			{
				rotation = input->Direction;
				movedirection = rotation;
			}

			if (input->AimingDirection.SqrMagnitude > FP._0)
			{
				rotation = input->AimingDirection;
			}

			bb->Set(f, Constants.IsAimingKey, input->IsShootButtonDown);
			bb->Set(f, Constants.AimDirectionKey, rotation);
			bb->Set(f, Constants.MoveDirectionKey, movedirection);
		}
	}
}