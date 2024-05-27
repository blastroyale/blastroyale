using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using Quantum;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public enum CollectionUnlockSource
	{
		Store, 
		Bp, 
		ServerGift,
		DefaultItem
	}
	
	public struct CollectionItemEquippedMessage : IMessage
	{
		public CollectionCategory Category;
		public ItemData EquippedItem;
	}
	
	public struct CollectionItemUnlockedMessage : IMessage
	{
		public CollectionUnlockSource Source;
		public ItemData EquippedItem;
	}
}