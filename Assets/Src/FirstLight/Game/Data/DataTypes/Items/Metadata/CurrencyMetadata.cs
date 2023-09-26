using System;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Represents an item that has a quantity
	/// </summary>
	[Serializable]
	public class CurrencyMetadata : IItemMetadata
	{
		public int Amount;
		public ItemMetadataType MetaType => ItemMetadataType.Currency;
		public override int GetHashCode() => Amount.GetHashCode();
	}
}