using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Models;
using JetBrains.Annotations;
using Quantum;

namespace FirstLight.Game.Logic
{
	public interface ICollectionDataProvider
	{
		List<CollectionItem> GetFullCollection(GameIdGroup group);

		List<CollectionItem> GetOwnedCollection(GameIdGroup group);

		[CanBeNull] CollectionItem GetEquipped(GameIdGroup group);

		GameIdGroup GetCollectionType(CollectionItem item);
	}

	public interface ICollectionLogic : ICollectionDataProvider
	{
		void Equip(GameIdGroup group, CollectionItem item);
	}
	
	public class CollectionLogic : AbstractBaseLogic<CollectionData>, ICollectionLogic, IGameLogicInitializer
	{
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

		public void Equip(GameIdGroup group, CollectionItem item)
		{
			Data.Equipped[group] = item;
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