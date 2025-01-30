using System;
using System.Collections.Generic;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Represents an item that holds information about a list of items that will be given to the player
	/// </summary>
	[System.Serializable]
	public class BundleMetadata : IItemMetadata
	{
		public string[] ProductIDs;
		public ItemMetadataType MetaType => ItemMetadataType.Bundle;
		public override int GetHashCode() => ProductIDs.GetHashCode();
	}
}