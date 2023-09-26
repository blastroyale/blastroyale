using System;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public class CollectionMetadata : IItemMetadata
	{
		public CollectionTrait [] Traits = null;
		public ItemMetadataType MetaType => ItemMetadataType.Collection;

		public override int GetHashCode()
		{
			var hash = 377;
			if (Traits == null) return hash;
			foreach (var m in Traits)
			{
				hash = unchecked(hash * 31 + m.GetHashCode());
			}
			return hash;
		} 
	}
}