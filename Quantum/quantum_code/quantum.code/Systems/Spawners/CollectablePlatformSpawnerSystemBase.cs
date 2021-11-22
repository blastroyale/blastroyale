namespace Quantum.Systems.Spawners
{
	/// <summary>
	/// This system is the abstract definition for a <see cref="CollectablePlatformSpawner"/> in the game.
	/// Implement this system and add it to the <see cref="SystemSetup"/> list in the correct order position for the
	/// defined intended platform spawner
	/// </summary>
	public abstract unsafe class CollectablePlatformSpawnerSystemBase : SystemMainThread,
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
					return;
				}
			}
		}

		protected bool IsReadyToSpawn(Frame f, CollectablePlatformSpawner* spawner)
		{
			return f.Time >= spawner->NextSpawnTime && spawner->Collectable == EntityRef.None;
		}
	}
}