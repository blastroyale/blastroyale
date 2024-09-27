using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct BattlePassLevelUpMessage : IMessage
	{
		public IEnumerable<ItemData> Rewards;
		public uint PreviousLevel;
		public uint NewLevel;
		public bool Completed;
	}

	public class TrophiesUpdatedMessage : IMessage
	{
		public ulong OldValue;
		public ulong NewValue;
		public uint Season;
	}
	
	/// <summary>
	/// Message for claiming rewards. Only works for the end of game claiming as BPP Fame and IAP has their own
	/// claiming messages and code :( 
	/// </summary>
	public class ClaimedRewardsMessage : IMessage
	{
		public List<ItemData> Rewards;
	}
	
	public class NewBattlePassSeasonMessage : IMessage
	{
	}

	public struct FinishedClaimingBpRewardsMessage : IMessage
	{
		
	}

	public struct OpenedCoreMessage : IMessage
	{
		public ItemData Core;
		public ItemData [] Results;
	}
	
	public class ItemRewardedMessage : IMessage
	{
		public ItemData Item;

		public ItemRewardedMessage(ItemData item)
		{
			Item = item;
		}
	}

	public struct GameCompletedRewardsMessage : IMessage
	{
		public MatchRewardsResult Rewards;
		public int TrophiesChange;
		public uint TrophiesBeforeChange;
	}
}