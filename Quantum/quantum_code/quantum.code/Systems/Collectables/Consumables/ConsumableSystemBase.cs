namespace Quantum.Systems.Collectables.Consumables
{
	/// <inheritdoc />
	/// <remarks>
	/// <see cref="Collectable"/> abstract implementation for the <see cref="Consumable"/> in the game.
	/// </remarks>
	public abstract class ConsumableSystemBase : CollectableSystemBase
	{
		protected abstract ConsumableType ConsumableType { get; }
		
		protected override void OnCollectablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, 
		                                            Collectable collectable)
		{
			var consumable = f.Get<Consumable>(e);

			OnConsumablePicked(f, e, playerEntity, player, consumable);
			f.Events.OnConsumablePicked(player, playerEntity, e, consumable);
			f.Signals.ConsumablePicked(player, e);
			f.Destroy(e);
		}

		protected override bool IsCorrectSystem(Frame f, EntityRef e, Collectable collectable)
		{
			return f.TryGet<Consumable>(e, out var consumable) && consumable.ConsumableType == ConsumableType;
		}
		
		protected abstract void OnConsumablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, 
		                                           Consumable consumable);
	}
}