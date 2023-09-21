using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using Equipment = Quantum.Equipment;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct BattlePassLevelUpMessage : IMessage
	{
		public List<KeyValuePair<UniqueId,Equipment>> Rewards;
		public uint newLevel;
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
		public List<RewardData> Rewards;
	}

	public struct FinishedClaimingBpRewardsMessage : IMessage
	{
		
	}
	
	public struct TutorialBattlePassCompleted : IMessage
	{
	}

	public struct GameCompletedRewardsMessage : IMessage
	{
		public List<RewardData> Rewards;
		public int TrophiesChange;
		public uint TrophiesBeforeChange;
	}
}