using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Services;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Messages
{
	public struct IapPurchaseSucceededMessage : IMessage
	{
		public ProductData Product;
		public RewardData ProductReward;
	}
	
	public struct IapPurchaseFailedMessage : IMessage
	{
		public ProductData Product;
		public PurchaseFailureReason FailureReason;
	}
}