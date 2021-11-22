using System;

namespace Backend.Data.DataTypes
{
	[Serializable]
	public struct EquipmentData : IEquatable<EquipmentData>
	{
		public UniqueId Id;
		public string Rarity;
		public uint Level;

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