namespace Quantum
{
	public unsafe partial struct Equipment
	{
		/// <summary>
		/// Requests if this current <see cref="Equipment"/> is a valid possible equipment
		/// </summary>
		public bool IsValid => Level > 0 && GameId != GameId.Random;
		
		public Equipment(GameId gameId, ItemRarity rarity, uint level)
		{
			GameId = gameId;
			Rarity = rarity;
			Level = level;
		}
	}
}