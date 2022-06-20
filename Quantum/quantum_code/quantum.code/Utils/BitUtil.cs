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
			// Magic algorithm from http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetNaive
			mask -= (mask >> 1) & 0x55555555;
			mask = (mask & 0x33333333) + ((mask >> 2) & 0x33333333);
			return ((mask + (mask >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
		}

		/// <summary>
		/// Returns the index (starting at the right side) of the <paramref name="n"/>'th set bit (from the left side).
		/// </summary>
		public static uint GetNthBitIndex(ulong mask, uint n)
		{
			// Magic algorithm from https://stackoverflow.com/questions/35316422/pick-random-bit-from-32bit-value-in-o1-if-possible
			ulong a = mask - ((mask >> 1) & ~0UL / 3);
			ulong b = (a & ~0UL / 5) + ((a >> 2) & ~0UL / 5);
			ulong c = (b + (b >> 4)) & ~0UL / 0x11;
			ulong d = (c + (c >> 8)) & ~0UL / 0x101;
			ulong t = ((d >> 32) + (d >> 48));
			ulong r = n + 1;
			ulong s = 64;

			s -= ((t - r) & 256) >> 3;
			r -= (t & ((t - r) >> 8));
			t = (d >> (int) (s - 16)) & 0xff;
			s -= ((t - r) & 256) >> 4;
			r -= (t & ((t - r) >> 8));
			t = (c >> (int) (s - 8)) & 0xf;
			s -= ((t - r) & 256) >> 5;
			r -= (t & ((t - r) >> 8));
			t = (b >> (int) (s - 4)) & 0x7;
			s -= ((t - r) & 256) >> 6;
			r -= (t & ((t - r) >> 8));
			t = (a >> (int) (s - 2)) & 0x3;
			s -= ((t - r) & 256) >> 7;
			r -= (t & ((t - r) >> 8));
			t = (mask >> (int) (s - 1)) & 0x1;
			s -= ((t - r) & 256) >> 8;

			return (uint) (s - 1);
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