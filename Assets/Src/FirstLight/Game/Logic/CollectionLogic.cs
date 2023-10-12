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
		
		/// <summary>
		/// Get Display name for given collection item
		/// </summary>
		string GetDisplayName(ItemData data);
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
		public IReadOnlyDictionary<CollectionCategory, List<ItemData>> DefaultCollectionItems => new ReadOnlyDictionary<CollectionCategory, List<ItemData>>(new Dictionary<CollectionCategory, List<ItemData>>()
		{
			{
				CollectionCategories.PROFILE_PICTURE, new List<ItemData>()
				{
					ItemFactory.Collection(GameId.Avatar1)
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
					ItemFactory.Collection(GameId.Falcon),
				}
			},
			{
				CollectionCategories.GRAVE, new List<ItemData>
				{
					ItemFactory.Collection(GameId.Tombstone),
				}
			},
			{
				CollectionCategories.MELEE_SKINS, new List<ItemData>
				{
					ItemFactory.Collection(GameId.MeleeSkinDefault),
				}
			}
		});

		/// <summary>
		/// If the player doesn't have an equipped it will return this values when the equipped item is requested
		/// If the player doesn't have the item, or there is no setting for the category it will get the first item of the collection
		/// </summary>
		public readonly Dictionary<CollectionCategory, ItemData> DefaultEquipped = new ()
		{
			{CollectionCategories.GLIDERS, ItemFactory.Collection(GameId.Falcon)},
			{CollectionCategories.GRAVE, ItemFactory.Collection(GameId.Tombstone)},
			{CollectionCategories.MELEE_SKINS, ItemFactory.Collection(GameId.MeleeSkinDefault)},
			{CollectionCategories.PROFILE_PICTURE, ItemFactory.Collection(GameId.Avatar1)},
		};


		public List<ItemData> GetFullCollection(CollectionCategory group)
		{
			List<ItemData> collection = new List<ItemData>();
			foreach (var id in group.Id.GetIds())
			{
				// Player can have multiple items marked as genericcollectionitem, this means the id represent multiple collectables 
				if(id.IsInGroup(GameIdGroup.GenericCollectionItem)) continue;
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

		public string GetDisplayName(ItemData data)
		{
			if (!data.Id.IsInGroup(GameIdGroup.GenericCollectionItem)) return data.Id.GetLocalization();
			// For generic items we cant depend on the game id, so for now display the collection type like "Corpos"
			if (data.TryGetMetadata<CollectionMetadata>(out var metadata) &&
				metadata.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection))
			{
				if (collection.Length > 0)
				{
					return collection[0].ToString().ToUpper() + collection[1..].ToLower();
				}
				return collection;
			}
			return "";
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
				if (IsItemOwned(defaultEquipped))
				{
					return defaultEquipped;
				}
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