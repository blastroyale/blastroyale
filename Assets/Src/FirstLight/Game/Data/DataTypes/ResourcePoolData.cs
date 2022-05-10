using System;
using FirstLight.Game.Ids;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public struct ResourcePoolData : IEquatable<ResourcePoolData>
	{
		public UniqueId Id;
		public ulong CurrentResourceAmountInPool;
		public DateTime LastPoolRefreshTime;
		
		public ResourcePoolData(UniqueId id, ulong currentResourceAmountInPool, DateTime lastPoolRefreshTime)
		{
			Id = id;
			CurrentResourceAmountInPool = currentResourceAmountInPool;
			LastPoolRefreshTime = lastPoolRefreshTime;
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
