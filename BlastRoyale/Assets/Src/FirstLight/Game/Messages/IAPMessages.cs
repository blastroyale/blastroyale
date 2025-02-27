using FirstLight.Game.Data.DataTypes;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct PurchaseClaimedMessage : IMessage
	{
		public ItemData ItemPurchased;
		public string SupportingContentCreator;
		public string PricePaid;
		public string PriceCurrencyId;
	}
	
	public struct BattlePassPurchasedMessage : IMessage
	{
	}


	public struct BattlePassLevelPurchasedMessage : IMessage
	{
		
	}
}