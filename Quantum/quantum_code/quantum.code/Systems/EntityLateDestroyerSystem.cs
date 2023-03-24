using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// It destroys any <see cref="EntityRef"/> at this moment that with the component <see cref="EntityDestroyer"/>.
	/// This can be helpful where we want to use entity data across the systems in the same frame after the entity was processed
	/// that needed to be destroyed.
	/// </summary>
	public unsafe class EntityLateDestroyerSystem : SystemMainThreadFilter<EntityLateDestroyerSystem.DestroyerFilter>
	{
		public struct DestroyerFilter
		{
			public EntityRef Entity;
			public EntityDestroyer* Component;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref DestroyerFilter filter)
		{
			if (filter.Component->time == FP._0 || filter.Component->time <= f.Time)
			{
				f.Destroy(filter.Entity);
			}
		}
	}
}