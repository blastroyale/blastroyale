using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.SDK.Services;
using Quantum;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Messages
{
	public struct IAPPurchaseCompletedMessage : IMessage
	{
		public List<KeyValuePair<UniqueId,Equipment>> Rewards;
	}

	public struct IAPPurchaseFailedMessage : IMessage
	{
		public PurchaseFailureReason Reason;
	}
}