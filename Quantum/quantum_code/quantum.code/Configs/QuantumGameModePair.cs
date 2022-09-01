using System;
using System.Collections.Generic;
using System.Text;

namespace Quantum
{
	[Serializable]
	public struct QuantumGameModePair<TValue>
	{
		public TValue Default;

		public List<string> Keys;
		public List<TValue> Values;

		public QuantumGameModePair(TValue @default, List<string> keys, List<TValue> values)
		{
			Default = @default;
			Keys = keys;
			Values = values;
		}

		/// <summary>
		/// Returns the default value of this pair.
		/// </summary>
		public TValue GetDefault()
		{
			return Default;
		}

		/// <summary>
		/// The value of this object for a specific game mode. If it doesn't exist it reutns <see cref="Default"/>.
		/// </summary>
		public TValue Get(string gameModeId)
		{
			var index = Keys.IndexOf(gameModeId);
			return index >= 0 ? Values[index] : GetDefault();
		}

		/// <inheritdoc cref="Get(string)"/>
		public TValue Get(Frame f)
		{
			return Get(f.Context.GameModeConfig.Id);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append("[");

			for (var i = 0; i < Keys.Count; i++)
			{
				sb.Append($"{Keys[i]} : {Values[i]}");
			}

			sb.Append("]");

			return sb.ToString();
		}
	}
}