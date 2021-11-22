using System;

namespace Quantum
{
	[Serializable]
	public struct QuantumPair<TKey, TValue>
	{
		public TKey Key;
		public TValue Value;

		public QuantumPair(TKey key, TValue value)
		{
			Key = key;
			Value = value;
		}
		
		public override string ToString()
		{
			return $"[{Key.ToString()},{Value.ToString()}]";
		}
	}
}