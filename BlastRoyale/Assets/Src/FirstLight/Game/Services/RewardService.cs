using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Presenters.Store;
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

		public UniTask<bool> ClaimRewardsAndWaitForRewardsScreenToClose(ItemData itemFilter = null);
		public UniTask<bool> OpenRewardScreen(ItemData item);
	}

	public class RewardService : IRewardService
	{
		private IGameDataProvider _data;
		private IGameServices _services;
		private Queue<OpenedCoreMessage> _unseenCores = new ();
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
			if (_services.UIService.IsScreenOpen<StoreScreenPresenter>()) return;
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

		/// <summary>
		/// Open the reward screen and wait for user to exit
		/// </summary>
		/// <returns>If the screen was open at all</returns>
		public async UniTask<bool> OpenRewardScreen(ItemData item)
		{
			FLog.Verbose("Opening rewards screen ");
			var waiter = new AsyncCallbackWrapper();
			await _services.UIService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData()
			{
				Items = new[] {item},
				SkipSummary = true,
				OnFinish = waiter.Invoke
			});
			await waiter.OnInvokeAsync();
			return true;
		}

		/// <summary>
		/// Opens the claim rewards screens and waits for it to be closed by the user
		/// </summary>
		/// <param name="itemFilter">If this is != null it will only try to claim this specific item</param>
		/// <returns>If the screen was open at all</returns>
		public async UniTask<bool> ClaimRewardsAndWaitForRewardsScreenToClose(ItemData itemFilter = null)
		{
			var waiter = new AsyncCallbackWrapper();
			if (itemFilter != null)
			{
				if (!_data.RewardDataProvider.UnclaimedRewards.Contains(itemFilter) && itemFilter.Id != GameId.Bundle)
				{
					FLog.Verbose($"Should not claim rewards, unclaimed rewards don't contain {itemFilter}");
					return false;
				}
			}

			FLog.Verbose("Opening rewards from RewardsService");
			var given = _services.CommandService.ExecuteCommandWithResult(new CollectUnclaimedRewardsCommand() {UncollectedReward = itemFilter});
			if (given is not {Count: > 0}) return false;
			await _services.UIService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData()
			{
				Items = given,
				SkipSummary = true,
				OnFinish = waiter.Invoke
			});
			await waiter.OnInvokeAsync();
			return true;
		}
	}
}