using Quantum.Systems.Spawners;

namespace Quantum.Systems
{
	/// <inheritdoc />
	/// <remarks>
	/// Implementation for the <see cref="ConsumablePlatformSpawner"/> to spawn <see cref="Consumable"/> in the game.
	/// </remarks>
	public unsafe class ConsumablePlatformSpawnerSystem : CollectablePlatformSpawnerSystemBase
	{
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			foreach (var spawner in f.GetComponentIterator<ConsumablePlatformSpawner>())
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

				f.Unsafe.GetPointer<Consumable>(entity)->Init(f, entity, transform->Position, transform->Rotation, config);
			}
		}

		private QuantumConsumableConfig GetConfig(Frame f, GameId id)
		{
			var configs = f.ConsumableConfigs.QuantumConfigs;

			return id == GameId.Random ? configs[f.RNG->Next(0, configs.Count)] : f.ConsumableConfigs.GetConfig(id);
		}
	}
}