using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct LoadoutUpdatedMessage : IMessage
	{
		public List<UniqueId> EquippedIds;
		public List<UniqueId> UnequippedIds;
	}
}