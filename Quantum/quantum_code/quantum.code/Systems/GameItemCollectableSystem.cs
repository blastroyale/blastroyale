namespace Quantum.Systems
{
	public unsafe class GameItemCollectableSystem : SystemSignalsOnly, ISignalOnConsumableCollected
	{
		public void OnConsumableCollected(Frame f, PlayerRef player, EntityRef entity, ConsumableType consumableType,
										  GameId collectableGameId)
		{
			if (consumableType != ConsumableType.GameItem) return;

			AddToMetaCollectedItems(f, collectableGameId, player, 1);
			f.Events.GameItemCollected(entity, player, collectableGameId, 1);
		}
		
		public static void AddToMetaCollectedItems(Frame f, GameId id, PlayerRef player , ushort amount)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			var collectedItems = gameContainer->PlayersData[player].CollectedMetaItems;
			var dict = f.ResolveDictionary(collectedItems);
			dict.TryGetValue(id, out var collected);
			collected += amount;
			dict[id] = collected;
		}
	}
	
	
}