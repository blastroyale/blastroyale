using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using Quantum;

namespace FirstLight.Game.Infos
{
	public struct ResourcePoolInfo
	{
		public GameId Id;
		public ResourcePoolConfig Config;
		public uint PoolCapacity;
		public uint CurrentAmount;
		public uint RestockPerInterval;
		public DateTime NextRestockTime;

		public bool IsFull => CurrentAmount == PoolCapacity;
	}
}