using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Represents an item that wont be in player inventory but unlock some feature
	/// </summary>
	[Serializable]
	public class UnlockMetadata : IItemMetadata
	{
		public UnlockSystem Unlock;
		public ItemMetadataType MetaType => ItemMetadataType.Unlock;
		public override int GetHashCode() => Unlock.GetHashCode();
	}
}