using System;
using Photon.Deterministic;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This static class process a deterministic random calculation value based on a predefined seed and state
	/// </summary>
	public static class Rng
	{
		/// <summary>
		/// Restores the current RNG state to the given <paramref name="count"/> based on the given <paramref name="seed"/>.
		/// The <paramref name="count "/> value can be defined for a state in the past or a state in the future.
		/// </summary>
		public static int[] Restore(int count, int seed)
		{
			var newState = RngUtils.GenerateRngState(seed);

			for (var i = 0; i < count; i++)
			{
				RngUtils.Next(newState);
			}

			return newState;
		}

		/// <summary>
		/// Requests a random generated <see cref="int"/> value between the given <paramref name="min"/> and <paramref name="max"/>,
		/// without changing the state with  the max value inclusive depending on the given <paramref name="maxInclusive"/>
		/// </summary>
		public static int Range(int min, int max, int[] rndState)
		{
			if (min > max)
			{
				throw new IndexOutOfRangeException("The min range value must be less than the max range value");
			}

			if (min == max)
			{
				return min;
			}

			var range = max - min;
			var value = RngUtils.Next(rndState);

			return (int) ((long) value * range / int.MaxValue + min);
		}

		/// <summary>
		/// Requests a random generated <see cref="int"/> value between the given <paramref name="min"/> and <paramref name="max"/>,
		/// without changing the state with  the max value inclusive depending on the given <paramref name="maxInclusive"/>
		/// </summary>
		public static FP Range(FP min, FP max, int[] rndState)
		{
			if (min > max)
			{
				throw new IndexOutOfRangeException("The min range value must be less than the max range value");
			}

			if (max > FP.UseableMax)
			{
				throw new IndexOutOfRangeException("The max range value must be less than FP.UseableMax");
			}

			if (FPMath.Abs(min - max) < FP.Epsilon)
			{
				return min;
			}

			var range = max - min;
			var value = (FP) RngUtils.Next(rndState);

			return range * value / int.MaxValue + min;
		}
	}

	/// <summary>
	/// Helper utility methods to manage the RNG data and behaviour
	/// Based on the .Net library Random class <see cref="https://referencesource.microsoft.com/#mscorlib/system/random.cs"/>
	/// </summary>
	public static class RngUtils
	{
		private const int _basicSeed = 161803398;
		private const int _stateLength = 56;
		private const int _helperInc = 21;
		private const int _valueIndex = 0;

		/// <summary>
		/// Creates a new state as an exact copy of the given <paramref name="state"/>.
		/// Use this method if you want to generate a new random number without changing the RNG current state.
		/// </summary>
		/// <exception cref="IndexOutOfRangeException">
		/// Thrown if the given <paramref name="state"/> does not have the length equal to <seealso cref="_stateLength"/>
		/// </exception>
		public static int[] CopyRngState(int[] state)
		{
			if (state == null || state.Length != _stateLength)
			{
				throw new IndexOutOfRangeException($"The Random data created has the wrong state date." +
					$"It should have a lenght of {_stateLength.ToString()} but has {state?.Length}");
			}

			var newState = new int[_stateLength];

			Array.Copy(state, newState, _stateLength);

			return newState;
		}

		/// <summary>
		/// Generates a completely new state rng state based on the given <paramref name="seed"/>.
		/// Based on the publish work of D.E. Knuth <see cref="https://www.informit.com/articles/article.aspx?p=2221790"/>
		/// </summary>
		public static int[] GenerateRngState(int seed)
		{
			var value = _basicSeed - (seed == int.MinValue ? int.MaxValue : Math.Abs(seed));
			var state = new int[_stateLength];

			state[_stateLength - 1] = value;
			state[_valueIndex] = 0;

			//Apparently the range [1..55] is special (Knuth)
			for (int i = 1, j = 1; i < _stateLength - 1; i++)
			{
				var index = (_helperInc * i) % (_stateLength - 1);

				state[index] = j;

				j = value - j;

				if (j < 0)
				{
					j += int.MaxValue;
				}

				value = state[index];
			}

			for (var k = 1; k < 5; k++)
			{
				for (var i = 1; i < _stateLength; i++)
				{
					state[i] -= state[1 + (i + 30) % (_stateLength - 1)];

					if (state[i] < 0)
					{
						state[i] += int.MaxValue;
					}
				}
			}

			return state;
		}

		/// <summary>
		/// Generates the next random number between [0...int.MaxValue] based on the given <paramref name="rndState"/>
		/// </summary>
		public static int Next(int[] rndState)
		{
			var index1 = rndState[_valueIndex] + 1;
			var index2 = index1 + _helperInc + 1;

			index1 = index1 < _stateLength ? index1 : 1;
			index2 = index2 < _stateLength ? index2 : 1;

			var ret = rndState[index1] - rndState[index2];

			ret = ret < 0 ? ret + int.MaxValue : ret;

			rndState[index1] = ret;
			rndState[_valueIndex] = index1;

			return ret;
		}
	}
}