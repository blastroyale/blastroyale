using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExitGames.Client.Photon.StructWrapping;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
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
		/// Default items gave to the player on account creation
		/// </summary>
		IReadOnlyDictionary<CollectionCategory, List<ItemData>> DefaultCollectionItems { get; }

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
		[CanBeNull]
		ItemData GetEquipped(CollectionCategory group);

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

		/// <summary>
		/// Check if player has all the default skins
		/// </summary>
		bool HasAllDefaultCollectionItems();
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

		/// <summary>
		/// Remove the collection item  from the player
		/// </summary>
		ItemData RemoveFromPlayer(ItemData item);
	}

	public static class DefaultCollectionItems
	{
		public static IReadOnlyDictionary<CollectionCategory, List<ItemData>> Items => new ReadOnlyDictionary<CollectionCategory, List<ItemData>>(new Dictionary<CollectionCategory, List<ItemData>>()
		{
			{
				CollectionCategories.PROFILE_PICTURE, new List<ItemData>()
				{
					ItemFactory.Collection(GameId.Avatar2)
				}
			},
			{
				CollectionCategories.PLAYER_SKINS, new List<ItemData>
				{
					ItemFactory.Collection(GameId.MaleAssassin),
				}
			},
			{
				CollectionCategories.GLIDERS, new List<ItemData>
				{
					ItemFactory.Collection(GameId.Turbine),
				}
			},
			{
				CollectionCategories.GRAVE, new List<ItemData>
				{
					ItemFactory.Collection(GameId.Demon),
				}
			},
			{
				CollectionCategories.MELEE_SKINS, new List<ItemData>
				{
					ItemFactory.Collection(GameId.MeleeSkinDefault),
				}
			}
		});
	}

	public class CollectionLogic : AbstractBaseLogic<CollectionData>, ICollectionLogic, IGameLogicInitializer
	{
		/// <summary>
		/// If the player doesn't have an equipped it will return this values when the equipped item is requested
		/// If the player doesn't have the item, or there is no setting for the category it will get the first item of the collection
		/// </summary>
		public readonly Dictionary<CollectionCategory, ItemData> DefaultEquipped = new ()
		{
			{CollectionCategories.GLIDERS, ItemFactory.Collection(GameId.Turbine)},
			{CollectionCategories.GRAVE, ItemFactory.Collection(GameId.Demon)},
			{CollectionCategories.MELEE_SKINS, ItemFactory.Collection(GameId.MeleeSkinDefault)},
			{CollectionCategories.PROFILE_PICTURE, ItemFactory.Collection(GameId.Avatar2)},
		};
		
		public IReadOnlyDictionary<CollectionCategory, List<ItemData>> DefaultCollectionItems => Logic.DefaultCollectionItems.Items;

		public List<ItemData> GetFullCollection(CollectionCategory group)
		{
			List<ItemData> collection = new List<ItemData>();
			foreach (var id in group.Id.GetIds())
			{
				// Player can have multiple items marked as genericcollectionitem, this means the id represent multiple collectables 
				if (id.IsInGroup(GameIdGroup.GenericCollectionItem)) continue;
				collection.Add(ItemFactory.Collection(id));
			}
			// Start to data driven shit

			return collection;
		}


		public List<ItemData> GetOwnedCollection(CollectionCategory group)
		{
			if (!Data.OwnedCollectibles.TryGetValue(group, out var collection))
			{
				collection = new ();
			}

			return collection;
		}

		public bool HasAllDefaultCollectionItems()
		{
			return DefaultCollectionItems.SelectMany(category => category.Value).All(IsItemOwned);
		}

		[CanBeNull]
		public ItemData GetEquipped(CollectionCategory group)
		{
			if (Data.Equipped.TryGetValue(group, out var equipped))
			{
				return equipped;
			}

			if (DefaultEquipped.TryGetValue(group, out var defaultEquipped))
			{
				return defaultEquipped;
			}

			var owned = GetOwnedCollection(group);
			return owned.Count > 0 ? owned[0] : null;
		}


		public ItemData UnlockCollectionItem(ItemData item)
		{
			var category = GetCollectionType(item);
			if (!Data.OwnedCollectibles.TryGetValue(category, out var collection))
			{
				collection = new ();
				Data.OwnedCollectibles[category] = collection;
			}

			if (!collection.Contains(item))
			{
				collection.Add(item);
			}

			return item;
		}

		public ItemData RemoveFromPlayer(ItemData item)
		{
			var category = GetCollectionType(item);
			if (!Data.OwnedCollectibles.TryGetValue(category, out var collection))
			{
				collection = new ();
				Data.OwnedCollectibles[category] = collection;
			}

			collection.Remove(item);

			if (Data.Equipped.TryGetValue(category, out var equipped))
			{
				if (equipped.Equals(item))
				{
					Data.Equipped.Remove(category);
				}
			}

			return item;
		}

		public CollectionCategory GetCollectionType(ItemData item)
		{
			return item.GetCollectionCategory();
		}


		public List<CollectionCategory> GetCollectionsCategories()
		{
			return new List<CollectionCategory>()
			{
				new (GameIdGroup.PlayerSkin),
				new (GameIdGroup.DeathMarker),
				new (GameIdGroup.Glider),
				new (GameIdGroup.MeleeSkin),
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