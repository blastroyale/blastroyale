using FirstLight.Game.Ids;
using FirstLight.SDK.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	public struct ItemScrappedMessage : IMessage
	{
		public UniqueId Id;
		public GameId GameId;
		public string Name;
		public float Durability;
		public Pair<GameId, uint> Reward;
	}
}