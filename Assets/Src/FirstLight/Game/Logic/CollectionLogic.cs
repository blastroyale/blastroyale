using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
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
		List<CollectionItem> GetFullCollection(CollectionCategory group);

		/// <summary>
		/// Get all items owned from a collection
		/// </summary>
		List<CollectionItem> GetOwnedCollection(CollectionCategory group);

		/// <summary>
		/// Get equipped item from a collection
		/// </summary>
		[CanBeNull] CollectionItem GetEquipped(CollectionCategory group);

		/// <summary>
		/// Get a collection type from a collection item
		/// </summary>
		CollectionCategory GetCollectionType(CollectionItem item);

		/// <summary>
		/// Get all available collections
		/// </summary>
		List<CollectionCategory> GetCollectionsCategories();

		/// <summary>
		/// Does the player own a specific item?
		/// </summary>
		bool IsItemOwned(CollectionItem item);
	}

	/// <summary>
	/// Handles collection items like earning & equipping
	/// </summary>
	public interface ICollectionLogic : ICollectionDataProvider
	{
		CollectionCategory Equip(CollectionItem item);
	}
	
	public class CollectionLogic : AbstractBaseLogic<CollectionData>, ICollectionLogic, IGameLogicInitializer
	{
		public List<CollectionItem> GetFullCollection(CollectionCategory group)
		{
			List<CollectionItem> collection = new List<CollectionItem>();
			foreach (var id in group.Id.GetIds())
			{
				collection.Add(new CollectionItem(id));
			}
			return collection;
		}

		public List<CollectionItem> GetOwnedCollection(CollectionCategory group)
		{
			if (!Data.Collections.TryGetValue(group, out var collection))
			{
				collection = new();
			}
			return collection;
		}

		[CanBeNull]
		public CollectionItem GetEquipped(CollectionCategory group)
		{
			Data.Equipped.TryGetValue(group, out var equipped);
			return equipped;
		}

		public CollectionCategory GetCollectionType(CollectionItem item)
		{
			return new (item.Id.GetGroups().First()); // TODO: this is shit
		}

		public List<CollectionCategory> GetCollectionsCategories()
		{
			return new List<CollectionCategory>()
			{
				new (GameIdGroup.PlayerSkin), new (GameIdGroup.DeathMarker), new (GameIdGroup.Glider)
			};
		}

		public bool IsItemOwned(CollectionItem item)
		{
			var group = GetCollectionType(item);
			return GetOwnedCollection(group).Contains(item);
		}

		public CollectionCategory Equip(CollectionItem item)
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