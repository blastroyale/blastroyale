using System.Collections.Generic;
using System.Linq;

namespace Quantum.Systems
{
	public class EntityGroupSystem : SystemSignalsOnly
	{
		public override void OnInit(Frame f)
		{
			DoSpawning(f);
		}


		private unsafe void DoSpawning(Frame f)
		{
			var topLevel = new HashSet<EntityRef>();
			var allChests = new List<EntityComponentPointerPair<EntityGroup>>();
			foreach (var chest in f.Unsafe.GetComponentBlockIterator<EntityGroup>())
			{
				allChests.Add(chest);
				topLevel.Add(chest.Entity);
			}

			foreach (var (_, component) in allChests)
			{
				var entities = f.ResolveList(component->Entities).ToList();
				foreach (var entityRef in entities.Where(f.Has<EntityGroup>))
				{
					topLevel.Remove(entityRef);
				}
			}

			var queue = new Queue<EntityComponentPointerPair<EntityGroup>>();
			foreach (var entityRef in topLevel)
			{
				queue.Enqueue(new EntityComponentPointerPair<EntityGroup>()
				{
					Entity = entityRef,
					Component = f.Unsafe.GetPointer<EntityGroup>(entityRef)
				});
			}

			while (queue.Count > 0)
			{
				var currentGroup = queue.Dequeue();
				var entities = f.ResolveList(currentGroup.Component->Entities).ToList();
				var toKeep = currentGroup.Component->EntitiesToKeep;

				// Get X random items from the entities list
				var toRemove = entities.Count - toKeep;
				for (var i = 0; i < toRemove; i++)
				{
					var index = f.RNG->Next(0, entities.Count);
					var entityToRemove = entities[index];
					entities.RemoveAt(index);
					DestroyEntity(f, entityToRemove);
				}

				foreach (var entityRef in entities)
				{
					if (f.Unsafe.TryGetPointer<EntityGroup>(entityRef, out var childGroup))
					{
						queue.Enqueue(new EntityComponentPointerPair<EntityGroup>()
						{
							Entity = entityRef,
							Component = childGroup
						});
					}
				}

				f.Destroy(currentGroup.Entity);
			}
		}

		/// <summary>
		/// Special handling for specific components
		/// </summary>
		private static unsafe void DestroyEntity(Frame f, EntityRef entity)
		{
			// CollectablePlatformSpawner
			if (f.Unsafe.TryGetPointer<CollectablePlatformSpawner>(entity, out var collectableSpawner))
			{
				collectableSpawner->Disabled = false;
			}

			// EntityGroup
			if (f.Unsafe.TryGetPointer<EntityGroup>(entity, out var entityGroup))
			{
				foreach (var egEntity in f.ResolveList(entityGroup->Entities))
				{
					DestroyEntity(f, egEntity);
				}
			}

			f.Destroy(entity);
		}
	}
}