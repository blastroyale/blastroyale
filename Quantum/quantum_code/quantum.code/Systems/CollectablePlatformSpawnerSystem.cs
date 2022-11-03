namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="CollectablePlatformSpawner"/>
	/// </summary>
	public unsafe class CollectablePlatformSpawnerSystem : SystemMainThreadFilter<CollectablePlatformSpawnerSystem.SpawnerFilter>,
	                                                       ISignalOnComponentAdded<CollectablePlatformSpawner>,
	                                                       ISignalOnComponentRemoved<Collectable> 
	{
		public struct SpawnerFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public CollectablePlatformSpawner* Spawner;
		}
		
		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, CollectablePlatformSpawner* component)
		{
			component->NextSpawnTime = f.Time + component->InitialSpawnDelayInSec;
		}
		
		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, Collectable* component)
		{
			foreach (var spawner in f.Unsafe.GetComponentBlockIterator<CollectablePlatformSpawner>())
			{
				if (spawner.Component->Collectable != entity)
				{
					continue;
				}
				
				spawner.Component->MarkCollected(f);
					
				// If RespawnTimeInSec == 0 then this is not a respawnable collectable so we destroy it
				if (spawner.Component->RespawnTimeInSec == 0)
				{
					f.Destroy(spawner.Entity);
				}
					
				return;
			}
		}

		public override void Update(Frame f, ref SpawnerFilter filter)
		{
			if (f.Time < filter.Spawner->NextSpawnTime || filter.Spawner->Collectable != EntityRef.None)
			{
				return;
			}
				
			filter.Spawner->Spawn(f, filter.Entity);
		}
	}
}