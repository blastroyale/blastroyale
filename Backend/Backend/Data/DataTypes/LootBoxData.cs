using System;

namespace Backend.Data.DataTypes
{
	[Serializable]
	public struct LootBoxData : IEquatable<LootBoxData>
	{
		public UniqueId Id;
		public int ConfigId;

		public LootBoxData(uint id, int configId)
		{
			Id = id;
			ConfigId = configId;
		}

		/// <inheritdoc />
		public bool Equals(LootBoxData other)
		{
			return Id.Equals(other.Id) && ConfigId == other.ConfigId;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is LootBoxData other && Equals(other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Id.GetHashCode();
				hashCode = (hashCode * 397) ^ ConfigId;
				return hashCode;
			}
		}
	}
	
	[Serializable]
	public struct TimedBoxData : IEquatable<TimedBoxData>
	{
		public UniqueId Id;
		public int ConfigId;
		public int Slot;	// The slot is 0 based
		public DateTime EndTime;
		public bool UnlockingStarted;

		public TimedBoxData(uint id, int configId, int slot, DateTime endTime, bool unlockingStarted)
		{
			Id = id;
			ConfigId = configId;
			Slot = slot;
			EndTime = endTime;
			UnlockingStarted = unlockingStarted;
		}

		/// <inheritdoc />
		public bool Equals(TimedBoxData other)
		{
			return Id.Equals(other.Id) && ConfigId == other.ConfigId;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is TimedBoxData other && Equals(other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Id.GetHashCode();
				hashCode = (hashCode * 397) ^ ConfigId;
				return hashCode;
			}
		}
	}
}