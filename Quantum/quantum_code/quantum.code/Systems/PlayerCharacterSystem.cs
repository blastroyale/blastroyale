using System;
using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="PlayerCharacter"/> and it's dependent component states
	/// </summary>
	public unsafe class PlayerCharacterSystem : SystemMainThreadFilter<PlayerCharacterSystem.PlayerCharacterFilter>,
	                                            ISignalOnPlayerDataSet, ISignalHealthIsZeroFromAttacker
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
		public void OnPlayerDataSet(Frame f, PlayerRef playerRef)
		{
			var playerEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id));
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var playerData = f.GetPlayerData(playerRef);
			var gridSquareSize = FP._1 * f.Map.WorldSize / f.Map.GridSizeX / FP._2;
			var spawnPosition = playerData.NormalizedSpawnPosition * f.Map.WorldSize +
			                    new FPVector2(f.RNG->Next(-gridSquareSize, gridSquareSize),
			                                  f.RNG->Next(-gridSquareSize, gridSquareSize));

			
			var spawnTransform = new Transform3D {Position = FPVector3.Zero, Rotation = FPQuaternion.Identity};
			var startingEquipment = f.Context.GameModeConfig.SpawnWithLoadout
				                        ? playerData.Loadout :
				                        Array.Empty<Equipment>();

			spawnTransform.Position = spawnPosition.XOY;

			playerCharacter->Init(f, playerEntity, playerRef, spawnTransform, playerData.PlayerLevel,
			                      playerData.PlayerTrophies, playerData.Skin, playerData.DeathMarker, startingEquipment,
			                      playerData.Loadout.FirstOrDefault(e => e.IsWeapon()));
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
			if (gameModeConfig.WeaponDeathDropStrategy >= DeathDropsStrategy.Normal && 
			    !playerDead->HasMeleeWeapon(f, entity))
			{
				Collectable.DropEquipment(f, playerDead->CurrentWeapon, deathPosition, step);
				step++;
			}

			// Try to drop Health pack
			if (gameModeConfig.HealthDeathDropStrategy >= DeathDropsStrategy.Normal &&
			    f.RNG->Next() <= f.GameConfig.DeathDropHealthChance)
			{
				Collectable.DropConsumable(f, GameId.Health, deathPosition, step, false);
				step++;
			}
			else if (gameModeConfig.HealthDeathDropStrategy == DeathDropsStrategy.NormalWithFallback)
			{
				Collectable.DropConsumable(f, GameId.AmmoSmall, deathPosition, step, false);
				step++;
			}

			var armourDropChance = f.RNG->Next();

			// Try to drop ShieldLarge
			if (gameModeConfig.ShieldDeathDropStrategy >= DeathDropsStrategy.Normal &&
			    armourDropChance <= f.GameConfig.DeathDropLargeShieldChance)
			{
				Collectable.DropConsumable(f, GameId.ShieldLarge, deathPosition, step, false);
			}
			else if (gameModeConfig.ShieldDeathDropStrategy == DeathDropsStrategy.NormalWithFallback &&
			         armourDropChance <= f.GameConfig.DeathDropSmallShieldChance)
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

			bb->Set(f, Constants.IsAimPressedKey, input->IsShootButtonDown);
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
			var mutatorState = f.Unsafe.GetPointerSingleton<GameContainer>()->MutatorsState;
			
			if (f.Time > mutatorState.HealthPerSecLastTime + seconds)
			{
				mutatorState.HealthPerSecLastTime = f.Time;
				
				if (!f.Unsafe.TryGetPointer<Stats>(filter.Entity, out var stats))
				{
					return;
				}

				if (health > 0)
				{
					stats->GainHealth(f, filter.Entity, new Spell(){ PowerAmount = (uint)health });
				}
				else
				{
					stats->ReduceHealth(f, filter.Entity, new Spell(){ PowerAmount = (uint)health });
				}
			}
		}
	}
}