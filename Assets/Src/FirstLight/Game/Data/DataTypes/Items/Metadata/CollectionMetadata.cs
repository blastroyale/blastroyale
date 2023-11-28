using System;
using System.Linq;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public class CollectionMetadata : IItemMetadata
	{
		public CollectionTrait[] Traits = null;
		public ItemMetadataType MetaType => ItemMetadataType.Collection;

		public bool TryGetTrait(string key, out string value)
		{
			value = Traits?.Where(t => t.Key == key).Select(t => t.Value).FirstOrDefault();
			return value != null;
		}

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