using FirstLight.Game.Data.DataTypes;
using FirstLight.SDK.Services;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Messages
{
	public struct RewardClaimedMessage : IMessage
	{
		public ItemData Reward;
	}

	public struct IAPPurchaseFailedMessage : IMessage
	{
		public PurchaseFailureReason Reason;
	}

	public struct BattlePassPurchasedMessage : IMessage
	{
	}
}