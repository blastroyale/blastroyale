using System;
using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Data
{

	[Serializable]
	public class CollectionItem : IEqualityComparer<CollectionItem>, IEquatable<CollectionItem>
	{
		public GameId Id;

		public CollectionItem(GameId id)
		{
			Id = id;
		}

		public bool Equals(CollectionItem x, CollectionItem y)
		{
			return x.Id == y.Id;
		}

		public int GetHashCode(CollectionItem obj)
		{
			return obj.Id.GetHashCode();
		}

		public bool Equals(CollectionItem other)
		{
			return Id == other.Id;
		}
	}

	[Serializable]
	public class CollectionData
	{
		public Dictionary<GameIdGroup, List<CollectionItem>> Collections = new()
		{
			{
				GameIdGroup.PlayerSkin, new List<CollectionItem>()
				{
					new CollectionItem(GameId.Male01Avatar), new CollectionItem(GameId.Female01Avatar),
					new CollectionItem(GameId.Male02Avatar), new CollectionItem(GameId.Female02Avatar),
				}
			}
		};

		public Dictionary<GameIdGroup, CollectionItem> Equipped = new()
		{
			{ GameIdGroup.PlayerSkin, new CollectionItem(GameId.Male01Avatar) }
		};
	}
}