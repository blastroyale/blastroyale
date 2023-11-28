using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Client-sided rewards stuff like making sure players see all rewards.
	/// </summary>
	public interface IRewardService
	{
		/// <summary>
		/// Gets the current unseen items of a given type
		/// </summary>
		public IReadOnlyCollection<ItemData> UnseenItems(ItemMetadataType type);

		/// <summary>
		/// Marks the given item as seen
		/// </summary>
		public void MarkAsSeen(ItemMetadataType type, ItemData item);
		
		/// <summary>
		/// Dequeues from a queue any cores the player have gotten recently that was not displayed to him 
		/// </summary>
		public bool TryGetUnseenCore(out OpenedCoreMessage msg);
	}

	public class RewardService : IRewardService
	{
		private IGameDataProvider _data;
		private IGameServices _services;
		private Queue<OpenedCoreMessage> _unseenCores = new();
		private Dictionary<ItemMetadataType, HashSet<ItemData>> _unseenItems = new ();
		
		public RewardService(IGameServices services, IGameDataProvider data)
		{
			_data = data;
			_services = services;
			_services.MessageBrokerService.Subscribe<OpenedCoreMessage>(OnOpenedCore);
			_services.MessageBrokerService.Subscribe<ItemRewardedMessage>(OnItemRewarded);
		}

		public IEnumerable<OpenedCoreMessage> UnseenOpenedCores => _unseenCores;

		private void OnItemRewarded(ItemRewardedMessage msg)
		{
			if (msg.Item.Id.IsInGroup(GameIdGroup.Collection))
			{
				if (!_unseenItems.TryGetValue(ItemMetadataType.Collection, out var unseenCategory))
				{
					unseenCategory = new HashSet<ItemData>();
					_unseenItems[ItemMetadataType.Collection] = unseenCategory;
				}
				unseenCategory.Add(msg.Item);
			}
		}
		
		private void OnOpenedCore(OpenedCoreMessage opened)
		{
			// Store still handles itself, for now
			if (_services.GameUiService.IsOpen<StoreScreenPresenter>()) return;
			_unseenCores.Enqueue(opened);
		}

		public void SetCoresSeen()
		{
			_unseenCores.Clear();
		}

		public IReadOnlyCollection<ItemData> UnseenItems(ItemMetadataType type)
		{
			if (!_unseenItems.TryGetValue(type, out var unseenCategory))
			{
				return Array.Empty<ItemData>();
			}
			return unseenCategory;
		}

		public void MarkAsSeen(ItemMetadataType type, ItemData item)
		{
			if (_unseenItems.TryGetValue(type, out var unseenCategory))
			{
				unseenCategory.Remove(item);
			}
		}

		public bool TryGetUnseenCore(out OpenedCoreMessage msg)
		{
			return _unseenCores.TryDequeue(out msg);
		}
	}
}