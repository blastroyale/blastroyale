using System;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Represents an equipment item
	/// </summary>
	[Serializable]
	public class EquipmentMetadata : IItemMetadata
	{
		public Equipment Equipment;
		public ItemMetadataType MetaType => ItemMetadataType.Equipment;
		public override int GetHashCode() => Equipment.GetServerHashCode();
	}

}