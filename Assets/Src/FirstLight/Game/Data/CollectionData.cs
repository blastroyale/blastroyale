using System;
using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Data
{

	[Serializable]
	public struct CollectionItem : IEqualityComparer<CollectionItem>, IEquatable<CollectionItem>
	{
		public GameId Id;

		public bool IsValid() => Id != GameId.Random;
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

		public override int GetHashCode()
		{
			int hash = 17;
			foreach (var collection in Collections.Values)
			{
				foreach (var item in collection)
				{
					hash = hash * 23 + item.GetHashCode();
				}
			}
			foreach (var item in Equipped.Values)
			{
				hash = hash * 23 + item.GetHashCode();
			}
			return hash;
		} 
	}
}