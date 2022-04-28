namespace Quantum
{
	public unsafe partial struct Equipment
	{
		/// <summary>
		/// Requests if this current <see cref="Equipment"/> is a valid possible equipment
		/// </summary>
		public bool IsValid => Level > 0 && GameId != GameId.Random;
		
		public Equipment(GameId gameId, ItemRarity rarity, ItemAdjective adjective, ItemMaterial material,
		                 ItemManufacturer manufacturer, ItemFaction faction, uint level, uint grade)
		{
			GameId = gameId;
			Rarity = rarity;
			Adjective = adjective;
			Material = material;
			Manufacturer = manufacturer;
			Faction = faction;
			Level = level;
			Grade = grade;
		}
	}
}