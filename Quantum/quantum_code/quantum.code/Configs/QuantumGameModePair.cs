using System;

namespace Quantum
{
	/// <summary>
	/// Stores two values for a configurable field, one for BattleRoyale,
	/// one for Deathmatch.
	/// </summary>
	[Serializable]
	public struct QuantumGameModePair<TValue>
	{
		public TValue BattleRoyale;
		public TValue Deathmatch;

		public QuantumGameModePair(TValue battleRoyale, TValue deathmatch)
		{
			BattleRoyale = battleRoyale;
			Deathmatch = deathmatch;
		}

		/// <summary>
		/// Returns <see cref="QuantumGameModePair{TValue}.BattleRoyale"/> for <see cref="GameMode.BattleRoyale"/>
		/// and <see cref="QuantumGameModePair{TValue}.Deathmatch"/> for <see cref="GameMode.Deathmatch"/>
		/// </summary>
		public TValue Get(GameMode mode)
		{
			switch (mode)
			{
				case GameMode.BattleRoyale:
					return BattleRoyale;
				case GameMode.Deathmatch:
					return Deathmatch;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}
		}

		/// <inheritdoc cref="Get(GameMode)"/>
		public TValue Get(Frame f)
		{
			return Get(f.Context.MapConfig.GameMode);
		}

		public override string ToString()
		{
			return $"[{BattleRoyale.ToString()},{Deathmatch.ToString()}]";
		}
	}
}