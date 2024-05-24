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
			int amt = 1;
			if (collectableGameId == GameId.COIN)
			{
				amt *= f.Context.GameModeConfig.MetaCoinsMultiplier;
			} else if (collectableGameId == GameId.BPP)
			{
				amt *= f.Context.GameModeConfig.MetaBppMultiplier;
			}
			collected += (ushort)amt;
			dict[collectableGameId] = collected;
			f.Events.GameItemCollected(entity, player, collectableGameId, (ushort)amt);
		}
	}
}