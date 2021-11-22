using System;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public struct AdventureData : IEquatable<AdventureData>
	{
		public int Id;
		public int KillCount;
		public bool RewardCollected;

		public AdventureData(int id, int killCount, bool rewardCollected)
		{
			Id = id;
			KillCount = killCount;
			RewardCollected = rewardCollected;
		}

		/// <inheritdoc />
		public bool Equals(AdventureData other)
		{
			return Id == other.Id;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is AdventureData other && Equals(other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return Id;
		}
	}
}