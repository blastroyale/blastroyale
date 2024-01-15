using Quantum.Core;

namespace Quantum.Systems
{
	public class EntityGroupSystem : SystemSignalsOnly, ISignalOnComponentAdded<EntityGroup>
	{
		public unsafe void OnAdded(Frame f, EntityRef entity, EntityGroup* component)
		{
			var entities = f.ResolveList(component->Entities);
			if (component->EntitiesToKeep < entities.Count)
			{
				// Get X random items from the entities list
				var toRemove = entities.Count - component->EntitiesToKeep;
				for (int i = 0; i < toRemove; i++)
				{
					var index = f.RNG->Next(0, entities.Count);
					var entityToRemove = entities[index];
					EntityDestroyer.Create(f, entityToRemove);
					entities.RemoveAt(index);

					HandleEntity(f, entityToRemove);
				}
			}

			EntityDestroyer.Create(f, entity);
		}

		/// <summary>
		/// Special handling for specific components
		/// </summary>
		private static unsafe void HandleEntity(FrameBase f, EntityRef entity)
		{
			// CollectablePlatformSpawner
			if(f.Unsafe.TryGetPointer<CollectablePlatformSpawner>(entity, out var collectableSpawner))
			{
				collectableSpawner->Disabled = false;
			}
		}
	}
}