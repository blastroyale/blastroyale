using System;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public struct ResourcePoolData : IEquatable<ResourcePoolData>
	{
		public GameId Id;
		public ulong CurrentResourceAmountInPool;
		public DateTime LastPoolRestockTime;
		
		public ResourcePoolData(GameId id, ulong currentResourceAmountInPool, DateTime lastPoolRestockTime)
		{
			Id = id;
			CurrentResourceAmountInPool = currentResourceAmountInPool;
			LastPoolRestockTime = lastPoolRestockTime;
		}
		
		/// <inheritdoc />
		public bool Equals(ResourcePoolData other)
		{
			return Id.Equals(other.Id);
		}
		
		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is ResourcePoolData other && Equals(other);
		}
		
		/// <inheritdoc />
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}
