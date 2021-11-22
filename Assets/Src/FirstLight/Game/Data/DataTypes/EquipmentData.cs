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
		public uint Level;

		public EquipmentData(UniqueId id, ItemRarity rarity, uint level)
		{
			Id = id;
			Rarity = rarity;
			Level = level;
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