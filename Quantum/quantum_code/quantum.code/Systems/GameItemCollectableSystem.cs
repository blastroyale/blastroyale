namespace Quantum.Systems
{
	public unsafe class GameItemCollectableSystem : SystemSignalsOnly, ISignalOnConsumableCollected
	{
		public void OnConsumableCollected(Frame f, PlayerRef player, EntityRef entity, Consumable consumable,
										  Collectable collectable)
		{
			if (consumable.ConsumableType != ConsumableType.GameItem) return;
			
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			var collectedItems = gameContainer->PlayersData[player].CollectedMetaItems;
			var dict = f.ResolveDictionary(collectedItems);
			dict.TryGetValue(collectable.GameId, out var collected);
			int amt = 1;
			if (collectable.GameId == GameId.COIN)
			{
				amt *= f.Context.GameModeConfig.MetaCoinsMultiplier;
			} else if (collectable.GameId == GameId.BPP)
			{
				amt *= f.Context.GameModeConfig.MetaBppMultiplier;
			}
			collected += (ushort)amt;
			dict[collectable.GameId] = collected;
			f.Events.GameItemCollected(entity, player, collectable.GameId, (ushort)amt);
		}
	}
}