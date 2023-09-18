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