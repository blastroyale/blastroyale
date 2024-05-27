using System;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using Photon.Deterministic;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the random generated values with always a deterministic result
	/// </summary>
	public interface IRngDataProvider
	{
		/// <summary>
		/// Returns the number of times the Rng has been counted;
		/// </summary>
		int Counter { get; }

		/// <summary>
		/// Requests the next <see cref="int"/> generated value without changing the state.
		/// Calling this multiple times in sequence gives always the same result.
		/// </summary>
		int Peek { get; }

		/// <summary>
		/// Requests the next <see cref="FP"/> generated value without changing the state.
		/// Calling this multiple times in sequence gives always the same result.
		/// </summary>
		FP PeekFp { get; }

		/// <remarks>
		/// Calling this multiple times with the same parameters in sequence gives always the same result.
		/// </remarks>
		int PeekRange(int min, int max);

		/// <remarks>
		/// Calling this multiple times with the same parameters in sequence gives always the same result.
		/// </remarks>
		[Obsolete("Use PeekFp")]
		FP PeekRange(FP min, FP max);
	}

	/// <inheritdoc />
	public interface IRngLogic : IRngDataProvider
	{
		/// <summary>
		/// Requests the next <see cref="int"/> generated value
		/// </summary>
		int Next { get; }

		/// <summary>
		/// Requests the next <see cref="FP"/> generated value (between 0 and 1 inclusive).
		/// </summary>
		FP NextFp { get; }

		/// <summary>
		/// Requests a value between min and max (exclusive).
		/// </summary>
		int Range(int min, int max);

		/// <summary>
		/// Requests a value between min and max (exclusive).
		///
		/// NOTE: This will not work correctly for values greater than <see cref="FP.UseableMax"/>.
		/// </summary>
		[Obsolete("Use NextFp")]
		FP Range(FP min, FP max);

		/// <summary>
		/// Restores the current RNG state to the given <paramref name="count"/>.
		/// The value can be defined for a state in the past or a state in the future.
		/// </summary>
		void Restore(int count);
	}

	/// <inheritdoc cref="IRngLogic"/>
	public class RngLogic : AbstractBaseLogic<RngData>, IRngLogic
	{
		public int Counter => Data.Count;
		public int Peek => PeekRange(0, int.MaxValue);
		public FP PeekFp => PeekRange(FP._0, FP._1);
		public int Next => Range(0, int.MaxValue);
		public FP NextFp => Range(FP._0, FP._1);

		public RngLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public int PeekRange(int min, int max)
		{
			return Rng.Range(min, max, RngUtils.CopyRngState(Data.State));
		}

		public int Range(int min, int max)
		{
			Data.Count++;

			return Rng.Range(min, max, Data.State);
		}

		public FP PeekRange(FP min, FP max)
		{
			return Rng.Range(min, max, RngUtils.CopyRngState(Data.State));
		}

		public FP Range(FP min, FP max)
		{
			Data.Count++;

			return Rng.Range(min, max, Data.State);
		}

		/// <inheritdoc />
		public void Restore(int count)
		{
			Data.Count = count;
			Data.State = Rng.Restore(count, Data.Seed);
		}
	}


	public static class RngExtensions
	{
		/// <summary>
		/// Return a random element from an array
		/// </summary>
		/// <returns></returns>
		public static T RandomElement<T>(this IRngLogic rng, T[] list)
		{
			return list[rng.Range(0, list.Length)];
		}
	}
}