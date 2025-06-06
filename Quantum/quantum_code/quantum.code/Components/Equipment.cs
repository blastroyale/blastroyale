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
		/// The default equipment weapon <see cref="GameId"/>
		/// </summary>
		public static GameId DefaultWeapon => GameId.Hammer;

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
						 uint maxDurability = 4,
						 uint initialReplicationCounter = 0,
						 uint tuning = 0,
						 uint level = 1,
						 uint generation = 0,
						 uint replicationCounter = 0,
						 uint totalRestoredDurability = 0,
						 long lastRepairTimestamp = 0)
		{
			GameId = gameId;

			Edition = edition;
			Rarity = rarity;
			Grade = grade;
			Faction = faction;
			Adjective = adjective;
			Material = material;
			Level = (ushort)level;
		
		}

		/// <summary>
		/// Checks if this current <see cref="Equipment"/> is a valid possible equipment.
		/// </summary>
		public readonly bool IsValid() => GameId != GameId.Random;

		/// <summary>
		/// Checks if the <see cref="GameId"/> belongs to the <see cref="GameIdGroup.Weapon"/> group.
		/// </summary>
		public bool IsWeapon() => GetEquipmentGroup() == GameIdGroup.Weapon;

		/// <summary>
		/// Checks if this item is the Hammer.
		/// </summary>
		public bool IsDefaultItem() => GameId == GameId.Hammer;

		/// <summary>
		/// Requests the equipment's current might
		/// </summary>
		public int GetTotalMight(Frame f)
		{
			return QuantumStatCalculator.GetMightOfItem(f.GameConfig, this);
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
			   Edition == other.Edition &&
			       Faction == other.Faction && GameId == other.GameId  &&
			       Grade == other.Grade &&
			       Level == other.Level && Material == other.Material;
		}

		/// <summary>
		/// Creates equipment on the given frame.
		/// Should be the source of truth for creating equipment inside the simulation.
		/// Player's loadout are defined outside in the game client and are passed down to the simulation.
		/// This loadout can be used for some items in the simulation such as box drops.
		/// </summary>
		public static Equipment Create(Frame f, GameId id, EquipmentRarity rarity, uint level)
		{
			return new Equipment
			{
				GameId = id,
				Rarity = rarity,
				Level = (ushort)level
			};
		}

		/// <summary>
		/// We need a server hash code to ignore dates until server and client is clock synced
		/// 
		/// </summary>
		public Int32 GetServerHashCode()
		{
			unchecked
			{
				var hash = 281;
				hash = hash * 31 + (Int32) Adjective;
				hash = hash * 31 + (Int32) Edition;
				hash = hash * 31 + (Int32) Faction;
				hash = hash * 31 + (Int32) GameId;
				hash = hash * 31 + (Int32) Grade;
				hash = hash * 31 + Level.GetHashCode();
				hash = hash * 31 + (Int32) Material;
				hash = hash * 31 + (Int32) Rarity;
				return hash;
			}
		}

		public override string ToString()
		{
			return
				$"Equipment({GameId}){{\n" +
				$"{nameof(Adjective)}: {Adjective},\n" +
				$"{nameof(Edition)}: {Edition},\n" +
				$"{nameof(Faction)}: {Faction}\n" +
				$"{nameof(Grade)}: {Grade}\n" +
				$"{nameof(Level)}: {Level}\n" +
				$"{nameof(Material)}: {Material}\n" +
				$"{nameof(Rarity)}: {Rarity}\n";
		}
	}
}
