namespace Quantum
{
	public static class GOAPDefaultHeuristic
	{
		public static int BitmaskDifferenceUInt32(GOAPState start, GOAPState end)
		{
			EWorldState positiveAchieved = start.Positive & end.Positive;
			EWorldState negativeAchieved = start.Negative & end.Negative;

			EWorldState positiveMissing = end.Positive & ~positiveAchieved;
			EWorldState negativeMissing = end.Negative & ~negativeAchieved;

			return CountOnesUInt32((uint) positiveMissing) + CountOnesUInt32((uint) negativeMissing);
		}

		public static int BitmaskDifferenceUInt64(GOAPState start, GOAPState end)
		{
			EWorldState positiveAchieved = start.Positive & end.Positive;
			EWorldState negativeAchieved = start.Negative & end.Negative;

			EWorldState positiveMissing = end.Positive & ~positiveAchieved;
			EWorldState negativeMissing = end.Negative & ~negativeAchieved;

			return CountOnesUInt64((ulong) positiveMissing) + CountOnesUInt64((ulong) negativeMissing);
		}

		public static int CountOnesUInt32(uint x)
		{
			x = x - ((x >> 1) & 0x55555555u);
			x = (x & 0x33333333u) + ((x >> 2) & 0x33333333u);
			x = (x + (x >> 4)) & 0x0F0F0F0Fu;

			return (int) ((x * 0x01010101u) >> 24);
		}

		public static int CountOnesUInt64(ulong x)
		{
			x = x - ((x >> 1) & 0x5555555555555555ul);
			x = (x & 0x3333333333333333ul) + ((x >> 2) & 0x3333333333333333ul);
			x = (x + (x >> 4)) & 0xF0F0F0F0F0F0F0Ful;

			return (int) (x * 0x101010101010101ul >> 56);
		}
	}
}