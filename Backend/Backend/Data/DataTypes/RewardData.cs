using System;

namespace Backend.Data.DataTypes
{
	[Serializable]
	public struct RewardData : IEquatable<RewardData>
	{
		public UniqueId RewardId;
		public uint Data;

		/// <inheritdoc />
		public bool Equals(RewardData other)
		{
			return RewardId == other.RewardId && Data == other.Data;
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
				return ((int) RewardId * 397) ^ (int) Data;
			}
		}
	}
}