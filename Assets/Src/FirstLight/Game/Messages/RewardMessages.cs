using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using Equipment = Quantum.Equipment;
using FirstLight.SDK.Services;
using Quantum;
using UnityEngine.Serialization;

namespace FirstLight.Game.Messages
{
	public struct BattlePassLevelUpMessage : IMessage
	{
		public IEnumerable<ItemData> Rewards;
		public uint NewLevel;
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

	public struct FinishedClaimingBpRewardsMessage : IMessage
	{
		
	}
	
	public struct TutorialBattlePassCompleted : IMessage
	{
	}

	public struct OpenedCoreMessage : IMessage
	{
		public ItemData Core;
		public ItemData [] Results;
	}

	public struct GameCompletedRewardsMessage : IMessage
	{
		public List<ItemData> Rewards;
		public int TrophiesChange;
		public uint TrophiesBeforeChange;
	}
}