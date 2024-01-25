using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quantum
{
	[Serializable]
	public struct GameModePair<TValue>
	{
		public string Key;
		public TValue Value;

		public GameModePair(string key, TValue value)
		{
			Key = key;
			Value = value;
		}

		public override string ToString()
		{
			return $"[{Key.ToString()},{Value.ToString()}]";
		}
	}

	[Serializable]
	public class QuantumPerGameModeConfig<TValue>
	{
		public TValue Default;
		public List<GameModePair<TValue>> Values = new List<GameModePair<TValue>>();


		/// <summary>
		/// Returns the default value of this pair.
		/// </summary>
		public TValue GetDefault()
		{
			return Default;
		}

		/// <summary>
		/// The value of this object for a specific game mode. If it doesn't exist it returns <see cref="Default"/>.
		/// </summary>
		public TValue Get(string gameModeId)
		{
			foreach (var gameModePair in Values.Where(gameModePair => gameModePair.Key == gameModeId))
			{
				return gameModePair.Value;
			}

			return Default;
		}

		/// <inheritdoc cref="Get(string)"/>
		public TValue Get(Frame f)
		{
			return Get(f.Context.GameModeConfig.Id);
		}
	}
}