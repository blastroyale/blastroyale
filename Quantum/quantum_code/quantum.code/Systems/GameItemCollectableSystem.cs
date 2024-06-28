namespace Quantum.Systems
{
	public unsafe class GameItemCollectableSystem : SystemSignalsOnly, ISignalOnConsumableCollected
	{
		public void OnConsumableCollected(Frame f, PlayerRef player, EntityRef entity, ConsumableType consumableType,
										  GameId collectableGameId)
		{
			if (consumableType != ConsumableType.GameItem) return;

			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			var collectedItems = gameContainer->PlayersData[player].CollectedMetaItems;
			var dict = f.ResolveDictionary(collectedItems);
			dict.TryGetValue(collectableGameId, out var collected);
			collected += 1;
			dict[collectableGameId] = collected;
			f.Events.GameItemCollected(entity, player, collectableGameId, 1);
		}
	}
}