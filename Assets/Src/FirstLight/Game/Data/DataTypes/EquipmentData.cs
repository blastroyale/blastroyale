using System;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public struct EquipmentData : IEquatable<EquipmentData>
	{
		public UniqueId Id;
		public ItemRarity Rarity;
		public ItemAdjective Adjective;
		public ItemMaterial Material;
		public ItemManufacturer Manufacturer;
		public ItemFaction Faction;
		public uint Level;
		public uint Grade;

		public EquipmentData(UniqueId id, ItemRarity rarity, ItemAdjective adjective, ItemMaterial material, ItemManufacturer manufacturer, ItemFaction faction, uint level, uint grade)
		{
			Id = id;
			Rarity = rarity;
			Adjective = adjective;
			Material = material;
			Manufacturer = manufacturer;
			Faction = faction;
			Level = level;
			Grade = grade;
		}
		
		/// <inheritdoc />
		public bool Equals(EquipmentData other)
		{
			return Id.Equals(other.Id);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is EquipmentData other && Equals(other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}