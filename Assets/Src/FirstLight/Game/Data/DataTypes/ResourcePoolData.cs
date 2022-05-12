using System;
using FirstLight.Game.Configs;
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

		public void Restock(ResourcePoolConfig config)
		{
			var minutesElapsedSinceLastRestock = (DateTime.UtcNow - LastPoolRestockTime).Minutes;
			var amountOfRestocks = (ulong) MathF.Floor(minutesElapsedSinceLastRestock / config.RestockIntervalMinutes);
			
			LastPoolRestockTime = DateTime.UtcNow;
			CurrentResourceAmountInPool += config.RestockPerInterval * amountOfRestocks;

			if (CurrentResourceAmountInPool > config.PoolCapacity)
			{
				CurrentResourceAmountInPool = config.PoolCapacity;
			}
		}

		public ulong Withdraw(ulong amountToWithdraw, ResourcePoolConfig config)
		{
			var amountWithdrawn = (ulong) 0;

			if (amountToWithdraw > CurrentResourceAmountInPool)
			{
				amountWithdrawn = CurrentResourceAmountInPool;
			}
			else
			{
				amountWithdrawn = amountToWithdraw;
			}

			// If withdrawing from full pool, the next restock timer needs to restarted, as opposed to ticking already.
			// When at max pool capacity, the player will see 'Storage Full' on the ResourcePoolWidget
			if (CurrentResourceAmountInPool >= config.PoolCapacity)
			{
				LastPoolRestockTime = DateTime.UtcNow;
			}

			CurrentResourceAmountInPool -= amountWithdrawn;
			return amountWithdrawn;
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
