using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
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
	
	public struct ItemUpgradedMessage : IMessage
	{
		public UniqueId Id;
		public GameId GameId;
		public string Name;
		public float Durability;
		public uint Level;
		public Pair<GameId, uint> Price;
	}
	
	public struct ItemRepairedMessage : IMessage
	{
		public UniqueId Id;
		public GameId GameId;
		public string Name;
		public float Durability;
		public float DurabilityFinal;
		public Pair<GameId, uint> Price;
	}
}