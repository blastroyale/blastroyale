namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="CollectablePlatformSpawner"/>
	/// </summary>
	public unsafe class CollectablePlatformSpawnerSystem : SystemMainThread,
	                                                       ISignalOnComponentAdded<CollectablePlatformSpawner>,
	                                                       ISignalOnComponentRemoved<Collectable> 
	{
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
				if (spawner.Component->Collectable == entity)
				{
					spawner.Component->MarkCollected(f);
					
					// If RespawnTimeInSec == 0 then this is not a respawnable collectable so we destroy it
					if (spawner.Component->RespawnTimeInSec == 0)
					{
						f.Remove<CollectablePlatformSpawner>(spawner.Entity);
						f.Add<EntityDestroyer>(spawner.Entity);
					}
					
					return;
				}
			}
		}
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			foreach (var spawner in f.Unsafe.GetComponentBlockIterator<CollectablePlatformSpawner>())
			{
				if (f.Time < spawner.Component->NextSpawnTime || spawner.Component->Collectable != EntityRef.None)
				{
					continue;
				}
				
				spawner.Component->Spawn(f, spawner.Entity);
			}
		}
	}
}