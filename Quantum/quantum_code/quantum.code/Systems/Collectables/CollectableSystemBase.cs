namespace Quantum.Systems.Collectables
{
	/// <summary>
	/// TODO: Remove this systems and move the code to the collectable itself
	/// </summary>
	public abstract class CollectableSystemBase : SystemSignalsOnly, ISignalCollectablePicked
	{
		/// <summary>
		/// Listen to collected picked signal
		/// </summary>
		public void CollectablePicked(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player)
		{
			if (!f.TryGet<Collectable>(entity, out var collectable) || !IsCorrectSystem(f, entity, collectable))
			{
				return;
			}

			OnCollectablePicked(f, entity, playerEntity, player, collectable);
			f.Events.OnLocalCollectableCollected(collectable.GameId, player, playerEntity, entity);
			f.Events.OnCollectableCollected(collectable.GameId, player, playerEntity, entity);
		}
		
		protected abstract void OnCollectablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, 
		                                            Collectable collectable);
		protected abstract bool IsCorrectSystem(Frame f, EntityRef e, Collectable collectable);
	}
}