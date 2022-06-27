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
}