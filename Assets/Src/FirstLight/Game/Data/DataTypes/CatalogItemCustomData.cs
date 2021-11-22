using System;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public struct CatalogItemCustomData
	{
		public GameId ItemGameId;
		public GameId RewardGameId;
		public GameId PriceGameId;
		public uint RewardValue;
		public float PriceValue;
	}
}