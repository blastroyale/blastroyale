using System;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public struct RewardData : IEquatable<RewardData>
	{
		public GameId RewardId;
		public int Value;

		public RewardData(GameId rewardId, int value)
		{
			RewardId = rewardId;
			Value = value;
		}

		/// <inheritdoc />
		public bool Equals(RewardData other)
		{
			return RewardId == other.RewardId && Value == other.Value;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is RewardData other && Equals(other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int) RewardId;
				hashCode = (hashCode * 397) ^ Value;
				return hashCode;
			}
		}
	}
}