using FirstLight.Game.Data.DataTypes;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct RewardClaimedMessage : IMessage
	{
		public ItemData Reward;
	}
	
	public struct BattlePassPurchasedMessage : IMessage
	{
	}


	public struct BattlePassLevelPurchasedMessage : IMessage
	{
		
	}
}