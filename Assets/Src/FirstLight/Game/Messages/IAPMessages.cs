using FirstLight.Services;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Messages
{
	public class IAPMessages
	{
		public struct IAPPurchaseCompleted : IMessage
		{
		}

		public struct IAPPurchaseFailed : IMessage
		{
			public PurchaseFailureReason Reason;
		}
	}
}