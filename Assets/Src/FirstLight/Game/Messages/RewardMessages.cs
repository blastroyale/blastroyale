using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct TrophyRoadRewardCollectingStartedMessage : IMessage
	{
		public uint Level;
	}
	
	public struct UnclaimedRewardsCollectingStartedMessage : IMessage
	{
	}
	
	public struct TrophyRoadRewardCollectedMessage : IMessage
	{
		public uint Level;
		public RewardData? Reward;
	}
	
	public struct UnclaimedRewardsCollectedMessage : IMessage
	{
		public List<RewardData> Rewards;
	}

	public struct AdventureFirstTimeRewardsCollectingStartedMessage : IMessage
	{
		public int AdventureId;
	}

	public struct AdventureFirstTimeRewardsCollectedMessage : IMessage
	{
		public int AdventureId;
		public List<RewardData> Rewards;
	}
	
	public struct GameCompletedRewardsMessage : IMessage
	{
		public List<RewardData> Rewards;
	}
}