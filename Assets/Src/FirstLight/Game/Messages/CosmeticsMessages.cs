using FirstLight.Game.Data;
using Quantum;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct CollectionItemEquippedMessage : IMessage
	{
		public CollectionCategory Category;
		public CollectionItem EquippedItem;
	}

	public struct PlayAnimationMessage : IMessage
	{
		public string AnimationName;
	}
	
}