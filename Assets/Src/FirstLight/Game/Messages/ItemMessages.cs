using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct UpdatedLoadoutMessage : IMessage
	{
		public List<UniqueId> EquippedIds;
		public List<UniqueId> UnequippedIds;
	}

	public struct ItemEquippedMessage : IMessage
	{
		public UniqueId ItemId;
	}

	public struct ItemUnequippedMessage : IMessage
	{
		public UniqueId ItemId;
	}
}