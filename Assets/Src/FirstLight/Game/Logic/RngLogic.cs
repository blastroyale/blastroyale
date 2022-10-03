using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using FirstLight.Services;
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

		/// <inheritdoc cref="Rng.Range(int,int,int[],bool)"/>
		/// <remarks>
		/// Calling this multiple times with the same parameters in sequence gives always the same result.
		/// </remarks>
		int PeekRange(int min, int max, bool maxInclusive = false);
		
		/// <inheritdoc cref="Rng.Range(FP,FP,int[],bool)"/>
		/// <remarks>
		/// Calling this multiple times with the same parameters in sequence gives always the same result.
		/// </remarks>
		FP PeekRange(FP min, FP max, bool maxInclusive = true);
	}

	/// <inheritdoc />
	public interface IRngLogic : IRngDataProvider
	{
		/// <summary>
		/// Requests the next <see cref="int"/> generated value
		/// </summary>
		int Next { get; }
		
		/// <summary>
		/// Requests the next <see cref="double"/> generated value
		/// </summary>
		FP NextFp { get; }

		/// <inheritdoc cref="Rng.Range(int,int,int[],bool)"/>
		int Range(int min, int max, bool maxInclusive = false);
		
		/// <inheritdoc cref="Rng.Range(FP,FP,int[],bool)"/>
		FP Range(FP min, FP max, bool maxInclusive = true);

		/// <summary>
		/// Restores the current RNG state to the given <paramref name="count"/>.
		/// The value can be defined for a state in the past or a state in the future.
		/// </summary>
		void Restore(int count);
	}

	/// <inheritdoc cref="IRngLogic"/>
	public class RngLogic : AbstractBaseLogic<RngData>, IRngLogic
	{
		/// <inheritdoc />
		public int Counter => Data.Count;

		/// <inheritdoc />
		public int Peek => PeekRange(0, int.MaxValue);
		
		/// <inheritdoc />
		public FP PeekFp => PeekRange(0, FP.MaxValue);

		/// <inheritdoc />
		public int Next => Range(0, int.MaxValue);
		
		/// <inheritdoc />
		public FP NextFp => Range(0, FP.MaxValue);
		
		public RngLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}
		
		/// <inheritdoc />
		public int PeekRange(int min, int max, bool maxInclusive = false)
		{
			return Rng.Range(min, max, RngUtils.CopyRngState(Data.State), maxInclusive);
		}

		/// <inheritdoc />
		public FP PeekRange(FP min, FP max, bool maxInclusive = true)
		{
			return Rng.Range(min, max, RngUtils.CopyRngState(Data.State), maxInclusive);
		}
		
		/// <inheritdoc />
		public int Range(int min, int max, bool maxInclusive = false)
		{
			Data.Count++;
			
			return Rng.Range(min, max, Data.State, maxInclusive);
		}

		/// <inheritdoc />
		public FP Range(FP min, FP max, bool maxInclusive = true)
		{
			Data.Count++;
			
			return Rng.Range(min, max, Data.State, maxInclusive);
		}

		/// <inheritdoc />
		public void Restore(int count)
		{
			Data.Count = count;
			Data.State = Rng.Restore(count, Data.Seed);
		}
	}
}