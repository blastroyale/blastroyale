using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.SDK.Services;
using Quantum;
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
}