using System;
using System.Collections.Generic;

namespace Quantum
{
	/// <summary>
	/// Holds (NFT) attributes about a piece of equipment (weapon or gear).
	/// </summary>
	[Serializable]
	public partial struct Equipment : IEquatable<Equipment>
	{
		private static readonly List<GameIdGroup> _slots = new List<GameIdGroup>
		{
			GameIdGroup.Amulet, GameIdGroup.Armor, GameIdGroup.Shield, GameIdGroup.Helmet, GameIdGroup.Weapon
		};
		
		/// <summary>
		/// An invalid piece of equipment
		/// </summary>
		public static Equipment None => new Equipment();

		/// <summary>
		/// Requests the list of <see cref="GameIdGroup"/> slots ready to be equipped
		/// </summary>
		public static IReadOnlyList<GameIdGroup> EquipmentSlots => _slots;

		/// <summary>
		/// Creates a new Equipment item with default (lowest) values, unless otherwise defined.
		/// </summary>
		public Equipment(GameId gameId,
		                 EquipmentEdition edition = EquipmentEdition.Genesis,
		                 EquipmentRarity rarity = EquipmentRarity.Common,
		                 EquipmentGrade grade = EquipmentGrade.GradeV,
		                 EquipmentFaction faction = EquipmentFaction.Order,
		                 EquipmentAdjective adjective = EquipmentAdjective.Regular,
		                 EquipmentMaterial material = EquipmentMaterial.Plastic,
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
		public bool IsWeapon() => GetEquipmentGroup() == GameIdGroup.Weapon;

		/// <summary>
		/// Checks if this item is the Hammer.
		///
		/// TODO: Might need different logic
		/// </summary>
		public bool IsDefaultItem() => GameId == GameId.Hammer;
		
		/// <summary>
		/// Requests the equipment's current might
		/// </summary>
		public int GetTotalMight(Frame f)
		{
			if (IsWeapon())
			{
				QuantumStatCalculator.CalculateWeaponStats(f, this, out var armour, out var health, out var speed, 
				                                           out var power, out var attackRange, out var pickupSpeed,
														   out var ammoCapacity);
				return QuantumStatCalculator.GetTotalMight(armour,health,speed, power, attackRange, pickupSpeed, ammoCapacity);
			}
			else
			{
				QuantumStatCalculator.CalculateGearStats(f, this, out var armour, out var health, out var speed, 
				                                         out var power, out var attackRange, out var pickupSpeed,
														 out var ammoCapacity);
				return QuantumStatCalculator.GetTotalMight(armour,health,speed, power, attackRange, pickupSpeed, ammoCapacity);
			}
		}

		/// <summary>
		/// Returns the "Equipment" <see cref="GameIdGroup"/> that this item belongs to.
		/// </summary>
		public GameIdGroup GetEquipmentGroup()
		{
			if (GameId.IsInGroup(GameIdGroup.Weapon)) return GameIdGroup.Weapon;
			if (GameId.IsInGroup(GameIdGroup.Helmet)) return GameIdGroup.Helmet;
			if (GameId.IsInGroup(GameIdGroup.Amulet)) return GameIdGroup.Amulet;
			if (GameId.IsInGroup(GameIdGroup.Armor)) return GameIdGroup.Armor;
			if (GameId.IsInGroup(GameIdGroup.Shield)) return GameIdGroup.Shield;

			throw new NotSupportedException($"Invalid Equipment GameId({GameId})");
		}

		public bool Equals(Equipment other)
		{
			return Equals(other, false);
		}

		public bool Equals(Equipment other, bool ignoreRarity)
		{
			return (ignoreRarity || Rarity == other.Rarity) && Adjective == other.Adjective &&
			       Durability == other.Durability && Edition == other.Edition &&
			       Faction == other.Faction && GameId == other.GameId && Generation == other.Generation &&
			       Grade == other.Grade && InitialReplicationCounter == other.InitialReplicationCounter &&
			       Level == other.Level && Manufacturer == other.Manufacturer && Material == other.Material &&
			       MaxDurability == other.MaxDurability && MaxLevel == other.MaxLevel &&
			       ReplicationCounter == other.ReplicationCounter && Tuning == other.Tuning;
		}
	}
}
