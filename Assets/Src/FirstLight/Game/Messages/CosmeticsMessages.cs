using FirstLight.Game.Data;
using Quantum;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public enum CollectionUnlockSource
	{
		Store, 
		Bp, 
		ServerGift	
	}
	
	public struct CollectionItemEquippedMessage : IMessage
	{
		public CollectionCategory Category;
		public CollectionItem EquippedItem;
	}
	
	public struct CollectionItemUnlockedMessage : IMessage
	{
		public CollectionUnlockSource Source;
		public CollectionItem EquippedItem;
	}
}