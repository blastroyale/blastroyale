using System;

namespace Quantum
{
	/// <summary>
	/// Some utility methods for manipulating bits. These allow you to treat an int like a HashSet<int>
	/// that has a max capacity of 32 and can only contain the numbers 0 to 31 inclusive.
	/// </summary>
	public static class BitUtil
	{
		/// <summary>
		/// Set the bit of the right-to-left 0-based index. 
		/// </summary>
		public static bool SetBit(ref int mask, int index)
		{
			var newMask = mask | (1 << index);
			if (newMask == mask)
			{
				return false;
			}

			mask = newMask;
			return true;
		}

		/// <summary>
		/// Value of the bit of the right-to-left 0-based index.
		/// </summary>
		public static bool IsBitSet(int mask, int index)
		{
			return (mask & (1 << index)) != 0;
		}

		/// <summary>
		/// Unset the bit of the right-to-left 0-based index. 
		/// </summary>
		public static bool UnsetBit(ref int mask, int index)
		{
			var newMask = mask & ~(1 << index);
			if (newMask == mask)
			{
				return false;
			}
			
			mask = newMask;
			return true;
		}
		
		/// <summary>
		/// Count the number of set bits. 
		/// </summary>
		public static int CountSetBits(int mask)
		{
			// Magic algorythm from http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetNaive
			mask -= (mask >> 1) & 0x55555555;
			mask = (mask & 0x33333333) + ((mask >> 2) & 0x33333333);
			return ((mask + (mask >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
		}

		/// <summary>
		/// Returns the index (starting at LSB) of the <paramref name="n"/>'th set bit.
		/// </summary>
		public static int GetNthBitIndex(int mask, int n)
		{
			int currentN = -1;
			
			for (int i = 0; i < 32; i++)
			{
				if ((mask & (1 << i)) != 0)
				{
					currentN++;
				}

				if (currentN == n)
				{
					return i;
				}
			}

			throw new NotSupportedException($"Trying get {n}th bit from {Convert.ToString(mask, 2)}");
		}
		
		/// <summary>
		/// Outputs the indexes (right-to-left and 0-based) of the set bits.
		/// </summary>
		public struct BitIterator
		{
			private int _copy;
			private int _count;

			public BitIterator(int mask)
			{
				_copy = mask;
				_count = 0;
			}
			
			public bool Next(out int output)
			{
				if (_copy == 0)
				{
					output = -1;
					return false;
				}
				
				while ((_copy & 1) == 0)
				{
					_copy >>= 1;
					_count++;
				}

				output = _count;
				_copy >>= 1;
				_count++;
				return true;
			}
		}
	}
}