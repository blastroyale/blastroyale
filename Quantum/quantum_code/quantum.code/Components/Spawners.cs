using System;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
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
			var transform = f.Unsafe.GetPointer<Transform2D>(e);
			
			if (GameId.IsInGroup(GameIdGroup.Consumable) || GameId.IsInGroup(GameIdGroup.Special) || GameId.IsInGroup(GameIdGroup.Currency))
			{
				Collectable = SpawnConsumable(f, GameId, transform, e);
			}
			else if (GameId.IsInGroup(GameIdGroup.Chest))
			{
				Collectable = SpawnChest(f, GameId, transform->Position, e);
			}
			else if (GameId == GameId.Random || GameId.IsInGroup(GameIdGroup.Weapon))
			{
				Collectable = SpawnWeapon(f, GameId, transform, e);
			}
			else if (GameId.IsInGroup(GameIdGroup.Equipment))
			{
				Collectable = SpawnGear(f, GameId, transform, e);
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
		private EntityRef SpawnConsumable(Frame f, GameId id, Transform2D* transform, EntityRef spawnerEntityRef)
		{
			var configs = f.ConsumableConfigs;
			var config = id == GameId.Random
				             ? configs.QuantumConfigs[f.RNG->Next(0, configs.QuantumConfigs.Count)]
				             : configs.GetConfig(id);
			var entity = f.Create(f.FindAsset<EntityPrototype>(config.AssetRef.Id));
			f.Unsafe.GetPointer<Consumable>(entity)->Init(f, entity, transform->Position, transform->Rotation, config, spawnerEntityRef, transform->Position);
			return entity;
		}

		/// <summary>
		/// Spawns a <see cref="EquipmentCollectable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		private EntityRef SpawnWeapon(Frame f, GameId id, Transform2D* transform, EntityRef spawnerEntityRef)
		{
			// TODO: Clean this up and merge with SpawnGear when we start spawning freelying gear for public
			var configs = f.WeaponConfigs;
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));
			var equipment = id == GameId.Random
				                ? gameContainer->GenerateNextWeapon(f)
								: Equipment.Create(f, configs.GetConfig(id).Id, EquipmentRarity.Common, 1);

			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, transform->Position, 0, transform->Position,
				equipment, spawnerEntityRef);

			return entity;
		}

		/// <summary>
		/// Spawns a <see cref="EquipmentCollectable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		private EntityRef SpawnGear(Frame f, GameId id, Transform2D* transform, EntityRef spawnerEntityRef)
		{
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));
			var equipment = Equipment.Create(f, id, EquipmentRarity.Common, 1);
			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, transform->Position, 0, transform->Position,
				equipment, spawnerEntityRef);

			return entity;
		}

		/// <summary>
		/// Spawns a <see cref="Chest"/> of the given <paramref name="id"/> in the given <paramref name="position"/>
		/// </summary>
		private EntityRef SpawnChest(Frame f, GameId id, FPVector2 position, EntityRef e)
		{
			// Don't spawn 50% of vitality chests if Midcore mutator is active
			if (id == GameId.ChestVitality
				&& f.Context.Mutators.HasFlagFast(Mutator.Midcore)
				&& f.RNG->Next() < FP._0_50)
			{
				Disabled = true;
				return EntityRef.None;
			}
			
			var chestEntity = ChestSystem.SpawnChest(f, id, position);
			if (f.Unsafe.TryGetPointer<ChestContents>(e, out var overrideComponent))
			{
				var contents = new ChestContents();
				contents.Items = overrideComponent->Items; // copy pointer not objects so same list for all
				f.Add(chestEntity, contents);
			}

			var config = f.ChestConfigs.GetConfig(id);
			if (config.AutoOpen)
			{
				Disabled = true;
				f.Unsafe.GetPointer<Chest>(chestEntity)->Open(f, chestEntity, EntityRef.None, PlayerRef.None);
				f.Destroy(chestEntity);
				return EntityRef.None;
			}
			return chestEntity;
		}
	}
}