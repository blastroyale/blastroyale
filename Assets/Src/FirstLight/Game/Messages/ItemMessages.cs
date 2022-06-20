using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	public struct UpdatedLoadoutMessage : IMessage
	{
		public IDictionary<GameIdGroup,UniqueId> SlotsUpdated;
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