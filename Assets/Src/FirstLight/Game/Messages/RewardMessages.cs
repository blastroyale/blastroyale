using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Services;
using Quantum;
using Equipment = Quantum.Equipment;

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
		public List<Equipment> Rewards;
		public uint newLevel;
	}

	public struct GameCompletedRewardsMessage : IMessage
	{
		public List<RewardData> Rewards;
		public int TrophiesChange;
		public uint TrophiesBeforeChange;
	}
}