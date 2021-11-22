using System;

namespace Quantum.Systems.Spawners
{
	/// <inheritdoc />
	/// <remarks>
	/// Implementation for the <see cref="WeaponPlatformSpawner"/> to spawn <see cref="WeaponCollectable"/> in the game.
	/// </remarks>
	public unsafe class WeaponPlatformSpawnerSystem : CollectablePlatformSpawnerSystemBase
	{
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			foreach (var spawner in f.GetComponentIterator<WeaponPlatformSpawner>())
			{
				var collectableSpawner = f.Unsafe.GetPointer<CollectablePlatformSpawner>(spawner.Entity);

				if (!IsReadyToSpawn(f, collectableSpawner))
				{
					continue;
				}
				var transform = f.Unsafe.GetPointer<Transform3D>(spawner.Entity);
				var config = GetConfig(f, collectableSpawner->GameId);
				var entity = f.Create(f.FindAsset<EntityPrototype>(config.AssetRef.Id));
				
				collectableSpawner->MarkSpawned(entity);

				f.Unsafe.GetPointer<WeaponCollectable>(entity)->Init(f, entity, transform->Position, transform->Rotation, config);
			}
		}

		private QuantumWeaponConfig GetConfig(Frame f, GameId id)
		{
			var configs = f.WeaponConfigs.QuantumConfigs;

			return id == GameId.Random ? configs[f.RNG->Next(0, configs.Count)] : f.WeaponConfigs.GetConfig(id);
		}
	}
}