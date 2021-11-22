namespace Quantum.Systems
{
	/// <summary>
	/// It destroys any <see cref="EntityRef"/> at this moment that with the component <see cref="EntityDestroyer"/>.
	/// This can be helpful where we want to use entity data across the systems in the same frame after the entity was processed
	/// that needed to be destroyed.
	/// </summary>
	public unsafe class EntityLateDestroyerSystem : SystemMainThread
	{
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			var it = f.GetComponentIterator<EntityDestroyer>();

			foreach (var filter in it)
			{
				f.Destroy(filter.Entity);
			}
		}
	}
}