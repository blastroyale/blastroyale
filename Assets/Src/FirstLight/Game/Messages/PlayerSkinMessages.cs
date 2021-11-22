using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	public struct PlayerSkinUpdatedMessage : IMessage
	{
		public GameId SkinId;
	}
}