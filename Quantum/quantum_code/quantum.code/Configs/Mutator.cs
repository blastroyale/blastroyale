using System;
using System.Linq;

namespace Quantum
{
	[Flags]
	public enum Mutator
	{
		None = 0,
		SpeedUp = 1 << 0,
		HealthyAir = 1 << 1,
		HammerTime = 1 << 2, // TODO: Remove when we have weapon filters
		DoNotDropSpecials = 1 << 3,
		ConsumableSharing = 1 << 4,
		DisableRevive = 1 << 5,
		Hardcore = 1 << 6,
		SafeZoneInPlayableArea = 1 << 7,
		Bloodthirst = 1 << 8
	}

	public static class MutatorExtensions
	{
		public static bool HasFlagFast(this Mutator value, Mutator flag)
		{
			return (value & flag) != 0;
		}

		public static Mutator[] GetSetFlags(this Mutator value)
		{
			if (value == Mutator.None) return Array.Empty<Mutator>();

			return Enum.GetValues(typeof(Mutator))
				.Cast<Mutator>()
				.Where(flag => value.HasFlagFast(flag))
				.ToArray();
		}

		public static int CountSetFlags(this Mutator flags)
		{
			int count = 0;
			int value = (int) flags;
			while (value != 0)
			{
				if ((value & 1) == 1)
				{
					count++;
				}

				value >>= 1;
			}

			return count;
		}
	}
}