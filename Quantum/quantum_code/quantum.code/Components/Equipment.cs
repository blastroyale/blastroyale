using System;
using System.Collections.Generic;

namespace Quantum
{
	/// <summary>
	/// Holds (NFT) attributes about a piece of equipment (weapon or gear).
	/// </summary>
	public partial struct Equipment : IEquatable<Equipment>
	{
		private static readonly List<GameIdGroup> _slots = new List<GameIdGroup>
		{
			GameIdGroup.Amulet, GameIdGroup.Armor, GameIdGroup.Chest, GameIdGroup.Helmet, GameIdGroup.Weapon
		};
		
		/// <summary>
		/// An invalid piece of equipment
		/// </summary>
		public static Equipment None => new Equipment();

		/// <summary>
		/// Requests the list of <see cref="GameIdGroup"/> slots ready to be equipped
		/// </summary>
		public static List<GameIdGroup> EquipmentSlots => _slots;

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
		public bool IsValid() => GameId != GameId.Random;

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

		public bool Equals(Equipment other)
		{
			return Adjective == other.Adjective && Durability == other.Durability && Edition == other.Edition &&
			       Faction == other.Faction && GameId == other.GameId && Generation == other.Generation &&
			       Grade == other.Grade && InitialReplicationCounter == other.InitialReplicationCounter &&
			       Level == other.Level && Manufacturer == other.Manufacturer && Material == other.Material &&
			       MaxDurability == other.MaxDurability && MaxLevel == other.MaxLevel && Rarity == other.Rarity &&
			       ReplicationCounter == other.ReplicationCounter && Tuning == other.Tuning;
		}
	}
}
