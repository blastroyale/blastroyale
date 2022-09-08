using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct UnclaimedRewardsCollectingStartedMessage : IMessage
	{
		public List<RewardData> Rewards;
	}

	public struct UnclaimedRewardsCollectedMessage : IMessage
	{
		public List<RewardData> Rewards;
	}

	public struct BattlePassLevelUpMessage : IMessage
	{
		public List<BattlePassRewardConfig> Rewards;
		public uint newLevel;
	}

	public struct GameCompletedRewardsMessage : IMessage
	{
		public List<RewardData> Rewards;
		public int TrophiesChange;
		public uint TrophiesBeforeChange;
	}
}