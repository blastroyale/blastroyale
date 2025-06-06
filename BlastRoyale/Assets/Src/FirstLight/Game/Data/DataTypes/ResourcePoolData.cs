using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public struct ResourcePoolData : IEquatable<ResourcePoolData>
	{
		public GameId Id;
		public uint CurrentResourceAmountInPool;
		public DateTime LastPoolRestockTime;
		
		public ResourcePoolData(GameId id, uint currentResourceAmountInPool, DateTime lastPoolRestockTime)
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
			int hash = 17;
			hash = hash * 23 + Id.GetHashCode();
			hash = hash * 23 + CurrentResourceAmountInPool.GetHashCode();
			return hash;
		}
	}
}
