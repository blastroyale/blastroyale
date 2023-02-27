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
		List<CollectionItem> GetFullCollection(GameIdGroup group);

		/// <summary>
		/// Get all items owned from a collection
		/// </summary>
		List<CollectionItem> GetOwnedCollection(GameIdGroup group);

		/// <summary>
		/// Get equipped item from a collection
		/// </summary>
		[CanBeNull] CollectionItem GetEquipped(GameIdGroup group);

		/// <summary>
		/// Get a collection type from a collection item
		/// </summary>
		GameIdGroup GetCollectionType(CollectionItem item);

		/// <summary>
		/// Get all available collections
		/// </summary>
		List<GameIdGroup> GetCollectionsCategories();
		
		/// <summary>
		/// Request the player's current skin
		/// </summary>
		IObservableFieldReader<GameId> PlayerSkin { get; }
	}

	/// <summary>
	/// Handles collection items like earning & equipping
	/// </summary>
	public interface ICollectionLogic : ICollectionDataProvider
	{
		GameIdGroup Equip(CollectionItem item);
	}
	
	public class CollectionLogic : AbstractBaseLogic<CollectionData>, ICollectionLogic, IGameLogicInitializer
	{
		/// <summary>
		/// Request the player's current skin
		/// </summary>
		private IObservableField<GameId> _playerSkin;
		public IObservableFieldReader<GameId> PlayerSkin => _playerSkin;
		
		public List<CollectionItem> GetFullCollection(GameIdGroup group)
		{
			List<CollectionItem> collection = new List<CollectionItem>();
			foreach (var id in group.GetIds())
			{
				collection.Add(new CollectionItem(id));
			}
			return collection;
		}

		public List<CollectionItem> GetOwnedCollection(GameIdGroup group)
		{
			if (!Data.Collections.TryGetValue(group, out var collection))
			{
				collection = new();
			}
			return collection;
		}

		[CanBeNull]
		public CollectionItem GetEquipped(GameIdGroup group)
		{
			Data.Equipped.TryGetValue(group, out var equipped);
			return equipped;
		}

		public GameIdGroup GetCollectionType(CollectionItem item)
		{
			return item.Id.GetGroups().First(); // TODO: this is shit
		}

		public List<GameIdGroup> GetCollectionsCategories()
		{
			return new List<GameIdGroup>()
			{
				GameIdGroup.PlayerSkin, GameIdGroup.DeathMarker, GameIdGroup.Glider
			};
		}

		public GameIdGroup Equip(CollectionItem item)
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
			/*
			_playerSkin = new ObservableResolverField<GameId>(() =>
			{
				var equipped = GetEquipped(GameIdGroup.PlayerSkin);
				return .Id;
			}, val => Data.PlayerSkinId = val);
			*/
		}

		public void ReInit()
		{
			/*
			 * 	var listeners = _playerSkin.GetObservers();
				_playerSkin = new ObservableResolverField<GameId>(() => Data.PlayerSkinId, val => Data.PlayerSkinId = val);
				_playerSkin.AddObservers(listeners);
				
					_playerSkin.InvokeUpdate();
			 */
		}
	}
}