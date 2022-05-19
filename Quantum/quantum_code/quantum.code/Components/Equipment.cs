namespace Quantum
{
	/// <summary>
	/// Holds (NFT) attributes about a piece of equipment (weapon or gear).
	/// </summary>
	public partial struct Equipment
	{
		/// <summary>
		/// Creates a new Equipment item with default (lowest) values, unless otherwise defined.
		/// </summary>
		public Equipment(GameId gameId,
		                 EquipmentEdition edition = EquipmentEdition.Genesis,
		                 EquipmentRarity rarity = EquipmentRarity.Common,
		                 EquipmentGrade grade = EquipmentGrade.GradeV,
		                 EquipmentFaction faction = EquipmentFaction.Order,
		                 EquipmentAdjective adjective = EquipmentAdjective.Regular,
		                 EquipmentMaterial material = EquipmentMaterial.Bronze,
		                 EquipmentManufacturer manufacturer = EquipmentManufacturer.Military,
		                 uint maxDurability = 100,
		                 uint maxLevel = 10,
		                 uint initialReplicationCounter = 0,
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
			MaxLevel = maxLevel;
			InitialReplicationCounter = initialReplicationCounter;
			Tuning = tuning;

			Level = level;
			Generation = generation;
			ReplicationCounter = replicationCounter;
			Durability = durability;
		}

		/// <summary>
		/// Checks if this current <see cref="Equipment"/> is a valid possible equipment.
		/// </summary>
		public bool IsValid() => Level > 0 && GameId != GameId.Random;

		/// <summary>
		/// Checks if this item is at <see cref="MaxLevel"/>.
		/// </summary>
		public bool IsMaxLevel() => Level >= MaxLevel;

		/// <summary>
		/// Checks if the <see cref="GameId"/> belongs to the <see cref="GameIdGroup.Weapon"/> group.
		/// </summary>
		public bool IsWeapon() => GameId.IsInGroup(GameIdGroup.Weapon);

		/// <summary>
		/// Checks if this item is the Hammer.
		///
		/// TODO: Might need different logic
		/// </summary>
		public bool IsDefaultItem() => GameId == GameId.Hammer;
	}
}