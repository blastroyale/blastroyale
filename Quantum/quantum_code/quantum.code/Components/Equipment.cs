namespace Quantum
{
	public unsafe partial struct Equipment
	{
		/// <summary>
		/// Requests if this current <see cref="Equipment"/> is a valid possible equipment
		/// </summary>
		public bool IsValid => Level > 0 && GameId != GameId.Random;

		/// <summary>
		/// Creates a new Equipment item with default (lowest) values, unless otherwise defined.
		/// </summary>
		public Equipment(GameId gameId,
		                 EquipmentEdition edition = EquipmentEdition.Genesis,
		                 EquipmentRarity rarity = EquipmentRarity.Common,
		                 EquipmentGrade grade = EquipmentGrade.GradeI,
		                 EquipmentFaction faction = EquipmentFaction.Order,
		                 EquipmentAdjective adjective = EquipmentAdjective.Regular,
		                 EquipmentMaterial material = EquipmentMaterial.Bronze,
		                 EquipmentManufacturer manufacturer = EquipmentManufacturer.Military,
		                 uint maxDurability = 100,
		                 uint tuning = 0,
		                 uint level = 0,
		                 uint generation = 0,
		                 uint replicationCounter = 0,
		                 uint durability = 100)
		{
			GameId = gameId;

			Edition = edition;
			Rarity = rarity;
			Grade = grade;
			Faction = faction;
			Adjective = adjective;
			Material = material;
			Manufacturer = manufacturer;

			MaxDurability = maxDurability;
			Tuning = tuning;

			Level = level;
			Generation = generation;
			ReplicationCounter = replicationCounter;
			Durability = durability;
		}
	}
}