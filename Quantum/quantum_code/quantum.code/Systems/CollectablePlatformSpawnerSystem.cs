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
			public Transform2D* Transform;
			public CollectablePlatformSpawner* Spawner;
		}

		/// <inheritdoc />
		public override void OnInit(Frame f)
		{
			var hammerTime = f.Context.Mutators.HasFlagFast(Mutator.HammerTime);
			var noHealthNoShields = f.Context.Mutators.HasFlagFast(Mutator.Hardcore);

			if (!hammerTime && !noHealthNoShields) return;

			foreach (var pair in f.Unsafe.GetComponentBlockIterator<CollectablePlatformSpawner>())
			{
				//TODO: instead of comparing to the weapon group, check against the hammertime params to see which drops to leave
				if (hammerTime && (pair.Component->GameId == GameId.Random || pair.Component->GameId.IsInGroup(GameIdGroup.Weapon)))
				{
					f.Destroy(pair.Entity);
				}
				else if (noHealthNoShields && (pair.Component->GameId == GameId.Health ||
							 pair.Component->GameId == GameId.ShieldSmall))
				{
					f.Destroy(pair.Entity);
				}
			}
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
			if (filter.Spawner->Disabled) return;
			if (f.Time < filter.Spawner->NextSpawnTime || filter.Spawner->Collectable != EntityRef.None)
			{
				return;
			}

			if (f.Has<EntityDestroyer>(filter.Entity)) return;

			filter.Spawner->Spawn(f, filter.Entity);

			if (!filter.Spawner->DoNotDestroy)
			{
				f.Destroy(filter.Entity);
			}
		}
	}
}