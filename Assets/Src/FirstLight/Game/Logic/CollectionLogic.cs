using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Models;
using JetBrains.Annotations;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// Obtains collection data like all items, owned items, equipped items
	/// </summary>
	public interface ICollectionDataProvider
	{
		/// <summary>
		/// Gets all items in a given collection group
		/// </summary>
		List<ItemData> GetFullCollection(CollectionCategory group);

		/// <summary>
		/// Get all items owned from a collection
		/// </summary>
		List<ItemData> GetOwnedCollection(CollectionCategory group);

		/// <summary>
		/// Get equipped item from a collection
		/// </summary>
		[CanBeNull] ItemData GetEquipped(CollectionCategory group);

		/// <summary>
		/// Get a collection type from a collection item
		/// </summary>
		CollectionCategory GetCollectionType(ItemData item);

		/// <summary>
		/// Get all available collections
		/// </summary>
		List<CollectionCategory> GetCollectionsCategories();

		/// <summary>
		/// Does the player own a specific item?
		/// </summary>
		bool IsItemOwned(ItemData item);
	}

	/// <summary>
	/// Handles collection items like earning & equipping
	/// </summary>
	public interface ICollectionLogic : ICollectionDataProvider
	{
		CollectionCategory Equip(ItemData item);
		
		/// <summary>
		/// Unlocks the collection item for the player
		/// </summary>
		ItemData UnlockCollectionItem(ItemData item);
		
	}
	
	public class CollectionLogic : AbstractBaseLogic<CollectionData>, ICollectionLogic, IGameLogicInitializer
	{
		public List<ItemData> GetFullCollection(CollectionCategory group)
		{
			List<ItemData> collection = new List<ItemData>();
			foreach (var id in group.Id.GetIds())
			{
				collection.Add(ItemFactory.Collection(id));
			}
			return collection;
		}

		public List<ItemData> GetOwnedCollection(CollectionCategory group)
		{
			if (!Data.OwnedCollectibles.TryGetValue(group, out var collection))
			{
				collection = new();
			}

			var defaultItemsForCollection = GetDefaultItemsForCollection(group);
			
			var l = new List<ItemData>(collection);
			l.AddRange(defaultItemsForCollection);
			return l;
		}

		public IEnumerable<ItemData> GetDefaultItemsForCollection(CollectionCategory group)
		{
			if (group == CollectionCategories.PROFILE_PICTURE)
			{
				var collection = new List<ItemData>();
				collection.Add(ItemFactory.Collection(GameId.Avatar1));
				return collection;
			}
			return Array.Empty<ItemData>();
		}

		[CanBeNull]
		public ItemData GetEquipped(CollectionCategory group)
		{
			if (Data.Equipped.TryGetValue(group, out var equipped))
			{
				return equipped;
			}

			Data.DefaultEquipped.TryGetValue(group, out var defaultEquipped);
			return defaultEquipped;
		}

		public ItemData UnlockCollectionItem(ItemData item)
		{
			var category = GetCollectionType(item);
			if (!Data.OwnedCollectibles.TryGetValue(category, out var collection))
			{
				collection = new();
				Data.OwnedCollectibles[category] = collection;
			}
			collection.Add(item);
			return item;
		}

		public CollectionCategory GetCollectionType(ItemData item)
		{
			return new (item.Id.GetGroups().First()); // TODO: this is shit
		}

		public List<CollectionCategory> GetCollectionsCategories()
		{
			return new List<CollectionCategory>()
			{
				new (GameIdGroup.PlayerSkin), 
				new (GameIdGroup.DeathMarker), 
				new (GameIdGroup.Glider),
				new (GameIdGroup.ProfilePicture)
			};
		}

		public bool IsItemOwned(ItemData item)
		{
			var group = GetCollectionType(item);
			return GetOwnedCollection(group).Contains(item);
		}

		public CollectionCategory Equip(ItemData item)
		{
			var group = GetCollectionType(item);
			if (!GetOwnedCollection(group).Contains(item))
			{
				throw new LogicException("Collection item not owned");
			}
			Data.Equipped[group] = item;
			return group;
		}
		
		public CollectionLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
		}

		public void ReInit()
		{
		}
	}
}