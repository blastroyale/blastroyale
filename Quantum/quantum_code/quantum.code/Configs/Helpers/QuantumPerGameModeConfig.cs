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
		private IDictionary<string, TValue> _cacheDictionary;

		private object _cacheLock = new object();

		/// <summary>
		/// Returns the default value of this pair.
		/// </summary>
		public TValue GetDefault()
		{
			return Default;
		}

		private void CheckCache()
		{
			if (_cacheDictionary == null)
			{
				lock (_cacheLock)
				{
					var dict = new Dictionary<string, TValue>();
					foreach (var config in Values)
					{
						dict.Add(config.Key, config.Value);
					}

					_cacheDictionary = dict;
				}
			}
		}

		/// <summary>
		/// The value of this object for a specific game mode. If it doesn't exist it returns <see cref="Default"/>.
		/// </summary>
		public TValue Get(string gameModeId)
		{
			CheckCache();
			if (_cacheDictionary.TryGetValue(gameModeId, out var config))
			{
				return config;
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