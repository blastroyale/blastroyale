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
			var count = 0;
			while (mask > 0)
			{
				mask &= (mask - 1);
				count++;
			}
			
			return count;
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