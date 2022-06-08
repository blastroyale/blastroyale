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

	public struct TempItemEquippedMessage : IMessage
	{
		public UniqueId ItemId;
	}

	public struct TempItemUnequippedMessage : IMessage
	{
		public UniqueId ItemId;
	}
}