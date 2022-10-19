using System.Collections.Generic;
using FirstLight.Services;
using Quantum;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Messages
{
	public struct IAPPurchaseCompletedMessage : IMessage
	{
		public List<Equipment> Rewards;
	}

	public struct IAPPurchaseFailedMessage : IMessage
	{
		public PurchaseFailureReason Reason;
	}
}