using System;
using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Data
{

	[Serializable]
	public struct CollectionCategory : IEqualityComparer<CollectionCategory>, IEquatable<CollectionCategory>
	{
		public GameIdGroup Id;

		public bool IsValid() => Id != GameIdGroup.GameDesign;
		public CollectionCategory(GameIdGroup id)
		{
			Id = id;
		}

		public bool Equals(CollectionCategory x, CollectionCategory y)
		{
			return x.Id == y.Id;
		}
		
		public static bool operator ==(CollectionCategory obj1, CollectionCategory obj2)
		{
			if (ReferenceEquals(obj1, obj2)) 
				return true;
			if (ReferenceEquals(obj1, null)) 
				return false;
			if (ReferenceEquals(obj2, null))
				return false;
			return obj1.Equals(obj2);
		}
		public static bool operator !=(CollectionCategory obj1, CollectionCategory obj2) => !(obj1 == obj2);
		
		public int GetHashCode(CollectionCategory obj)
		{
			return obj.Id.GetHashCode();
		}

		public bool Equals(CollectionCategory other)
		{
			if (ReferenceEquals(other, null))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Id == other.Id;
		}
	}
	
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
		public Dictionary<CollectionCategory, List<CollectionItem>> Collections = new()
		{
			{
				new (GameIdGroup.PlayerSkin), new List<CollectionItem>()
				{
					new(GameId.Male01Avatar), new(GameId.Female01Avatar),
					new(GameId.Male02Avatar), new(GameId.Female02Avatar),
					new(GameId.MaleAssassin),
				}
			}
		};

		public Dictionary<CollectionCategory, CollectionItem> Equipped = new()
		{
			{ new(GameIdGroup.PlayerSkin), new(GameId.Male01Avatar) }
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