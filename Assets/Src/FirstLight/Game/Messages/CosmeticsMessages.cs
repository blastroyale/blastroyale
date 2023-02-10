using Quantum;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct PlayerSkinUpdatedMessage : IMessage
	{
		public GameId SkinId;
	}
}