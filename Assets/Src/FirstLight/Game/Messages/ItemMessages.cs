using FirstLight.Game.Ids;
using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct ItemEquippedMessage : IMessage
	{
		public UniqueId ItemId;
	}

	public struct ItemUnequippedMessage : IMessage
	{
		public UniqueId ItemId;
	}

	public struct ItemUpgradedMessage : IMessage
	{
		public UniqueId ItemId;
		public uint PreviousLevel;
		public uint NewLevel;
	}
}