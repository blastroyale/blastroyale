using System;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct PlayerSpawner
	{
		/// <summary>
		/// Requests the state of the PlayerSpawner. The spawner can be either activated and available to spawn a new
		/// player in it's position or not available for it.
		/// </summary>
		public bool IsActive(Frame f)
		{
			return f.Time >= ActivationTime;
		}
		
	}

	public unsafe partial struct CollectablePlatformSpawner
	{
		/// <summary>
		/// Requests the interval time to the next collectable to be spawn
		/// </summary>
		public FP IntervalTime => SpawnCount == 0 ? InitialSpawnDelayInSec : RespawnTimeInSec;

		/// <summary>
		/// Mark collectable entity as not spawned and set the next spawn time
		/// </summary>
		internal void MarkCollected(Frame f)
		{
			Collectable = EntityRef.None;
			NextSpawnTime = f.Time + RespawnTimeInSec;
		}

		/// <summary>
		/// Spawns a <see cref="Collectable"/> from this spawners <see cref="GameId"/>.
		/// </summary>
		internal void Spawn(Frame f, EntityRef e)
		{
			var transform = f.Get<Transform3D>(e);

			if (GameId.IsInGroup(GameIdGroup.Consumable) || GameId.IsInGroup(GameIdGroup.Special))
			{
				Collectable = SpawnConsumable(f, GameId, &transform, e);
			}
			else if (GameId.IsInGroup(GameIdGroup.Chest))
			{
				Collectable = SpawnChest(f, GameId, transform.Position, e);
			}
			else if (GameId == GameId.Random || GameId.IsInGroup(GameIdGroup.Weapon))
			{
				Collectable = SpawnWeapon(f, GameId, RarityModifier, &transform, e);
			}
			else if (GameId.IsInGroup(GameIdGroup.Equipment))
			{
				Collectable = SpawnGear(f, GameId, RarityModifier, &transform, e);
			}
			else
			{
				throw new
					InvalidOperationException($"The platform spawner is missing the component for the given {e} " +
					                          $"entity of the given {GameId} id");
			}

			SpawnCount++;
		}

		/// <summary>
		/// Spawns a <see cref="Consumable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		private EntityRef SpawnConsumable(Frame f, GameId id, Transform3D* transform, EntityRef spawnerEntityRef)
		{
			var configs = f.ConsumableConfigs;
			var config = id == GameId.Random
				             ? configs.QuantumConfigs[f.RNG->Next(0, configs.QuantumConfigs.Count)]
				             : configs.GetConfig(id);
			var entity = f.Create(f.FindAsset<EntityPrototype>(config.AssetRef.Id));

			f.Unsafe.GetPointer<Consumable>(entity)->Init(f, entity, transform->Position, transform->Rotation, ref config, spawnerEntityRef, transform->Position);

			return entity;
		}

		/// <summary>
		/// Spawns a <see cref="EquipmentCollectable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		private EntityRef SpawnWeapon(Frame f, GameId id, int rarityModifier, Transform3D* transform, EntityRef spawnerEntityRef)
		{
			// TODO: Clean this up and merge with SpawnGear when we start spawning freelying gear for public
			var configs = f.WeaponConfigs;
			var gameContainer = f.GetSingleton<GameContainer>();
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));
			var rarity = (EquipmentRarity) FPMath.Clamp((int) gameContainer.DropPool.AverageRarity + rarityModifier,
			                                            0,
			                                            (int) EquipmentRarity.TOTAL - 1);

			var equipment = id == GameId.Random
				                ? gameContainer.GenerateNextWeapon(f)
								: Equipment.Create(f, configs.GetConfig(id).Id, rarity, 1);

			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, transform->Position, FPQuaternion.Identity, transform->Position,
			                                                        ref equipment, spawnerEntityRef);

			return entity;
		}

		/// <summary>
		/// Spawns a <see cref="EquipmentCollectable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		private EntityRef SpawnGear(Frame f, GameId id, int rarityModifier, Transform3D* transform, EntityRef spawnerEntityRef)
		{
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));
			var equipment = Equipment.Create(f, id, EquipmentRarity.Common, 1);

			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, transform->Position, FPQuaternion.Identity, transform->Position,
			                                                        ref equipment, spawnerEntityRef);

			return entity;
		}

		/// <summary>
		/// Spawns a <see cref="Chest"/> of the given <paramref name="id"/> in the given <paramref name="position"/>
		/// </summary>
		public static EntityRef SpawnChest(Frame f, GameId id, FPVector3 position, EntityRef e)
		{
			var config = f.ChestConfigs.GetConfig(id);
			var chestEntity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.ChestPrototype.Id));
			
			if (f.Unsafe.TryGetPointer<ChestOverride>(e, out var overrideComponent))
			{
				overrideComponent->CopyComponent(f, chestEntity, e, overrideComponent);
			}
			f.Unsafe.GetPointer<Chest>(chestEntity)->Init(f, chestEntity, position, FPQuaternion.Identity, ref config);

			return chestEntity;
		}
	}
}