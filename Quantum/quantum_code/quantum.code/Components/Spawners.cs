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

	public unsafe partial struct ConsumablePlatformSpawner
	{
		/// <summary>
		/// Spawns a <see cref="Consumable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		internal EntityRef Spawn(Frame f, GameId id, Transform3D transform)
		{
			var configs = f.ConsumableConfigs;
			var config = id == GameId.Random ? configs.QuantumConfigs[f.RNG->Next(0, configs.QuantumConfigs.Count)] : configs.GetConfig(id);
			var entity = f.Create(f.FindAsset<EntityPrototype>(config.AssetRef.Id));
			
			QuantumHelpers.TryFindPosOnNavMesh(f, transform.Position, out FPVector3 newPosition);
			
			f.Unsafe.GetPointer<Consumable>(entity)->Init(f, entity, newPosition, transform.Rotation, config);

			return entity;
		}
	}

	public unsafe partial struct WeaponPlatformSpawner
	{
		/// <summary>
		/// Spawns a <see cref="WeaponCollectable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		internal EntityRef Spawn(Frame f, GameId id, Transform3D transform)
		{
			var configs = f.WeaponConfigs;
			var config = id == GameId.Random ? configs.QuantumConfigs[f.RNG->Next(0, configs.QuantumConfigs.Count)] : configs.GetConfig(id);
			var entity = f.Create(f.FindAsset<EntityPrototype>(config.AssetRef.Id));
			
			QuantumHelpers.TryFindPosOnNavMesh(f, transform.Position, out FPVector3 newPosition);
			
			f.Unsafe.GetPointer<WeaponCollectable>(entity)->Init(f, entity, newPosition, transform.Rotation, config);

			return entity;
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
		/// Spawns a <see cref="Collectable"/> of the given <paramref name="id"/> in the given <paramref name="transform"/>
		/// </summary>
		internal void Spawn(Frame f, EntityRef e)
		{
			var transform = f.Get<Transform3D>(e);
			
			if (f.Unsafe.TryGetPointer<ConsumablePlatformSpawner>(e, out var consumablePlatformSpawner))
			{
				Collectable = consumablePlatformSpawner->Spawn(f, GameId, transform);
			}
			else if(f.Unsafe.TryGetPointer<WeaponPlatformSpawner>(e, out var weaponPlatformSpawner))
			{
				Collectable = weaponPlatformSpawner->Spawn(f, GameId, transform);
			}
			else
			{
				throw new InvalidOperationException($"The platform spawner is missing the component for the given {e} " +
				                                    $"entity of the given {GameId} id");
			}
			
			SpawnCount++;
			
		}
	}
}