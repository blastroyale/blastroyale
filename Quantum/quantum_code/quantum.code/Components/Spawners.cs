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

			if (GameId.IsInGroup(GameIdGroup.Consumable))
			{
				Collectable = SpawnConsumable(f, GameId, transform);
			}
			else if (GameId.IsInGroup(GameIdGroup.Chest))
			{
				Collectable = SpawnChest(f, GameId, transform);
			}
			else if (GameId == GameId.Random || GameId.IsInGroup(GameIdGroup.Weapon))
			{
				Collectable = SpawnWeapon(f, GameId, RarityModifier, transform);
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
		private EntityRef SpawnConsumable(Frame f, GameId id, Transform3D transform)
		{
			var configs = f.ConsumableConfigs;
			var config = id == GameId.Random
				             ? configs.QuantumConfigs[f.RNG->Next(0, configs.QuantumConfigs.Count)]
				             : configs.GetConfig(id);
			var entity = f.Create(f.FindAsset<EntityPrototype>(config.AssetRef.Id));

			f.Unsafe.GetPointer<Consumable>(entity)->Init(f, entity, transform.Position, transform.Rotation, config);

			return entity;
		}

		/// <summary>
		/// Spawns a <see cref="EquipmentCollectable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		private EntityRef SpawnWeapon(Frame f, GameId id, int rarityModifier, Transform3D transform)
		{
			// TODO: Clean this up when we start spawning gear
			var configs = f.WeaponConfigs;
			var medianRarity = f.Context.GetMedianRarity(f);
			var offhandPool = f.Context.GetOffhandPool(f);

			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.EquipmentPickUpPrototype.Id));
			var rarity = (EquipmentRarity) FPMath.Clamp((int) medianRarity + rarityModifier,
			                                            0,
			                                            (int) EquipmentRarity.TOTAL - 1);
			var equipment = id == GameId.Random
				                ? offhandPool[f.RNG->Next(0, offhandPool.Length)]
				                : new Equipment(configs.GetConfig(id).Id, rarity: rarity);

			f.Unsafe.GetPointer<EquipmentCollectable>(entity)->Init(f, entity, transform.Position, transform.Rotation,
			                                                        equipment);

			return entity;
		}

		/// <summary>
		/// Spawns a <see cref="Chest"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		private EntityRef SpawnChest(Frame f, GameId id, Transform3D transform)
		{
			var config = f.ChestConfigs.GetConfig(id);
			var entity = f.Create(f.FindAsset<EntityPrototype>(config.AssetRef.Id));

			f.Unsafe.GetPointer<Chest>(entity)->Init(f, entity, transform.Position, transform.Rotation, config);

			return entity;
		}
	}
}