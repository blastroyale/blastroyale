using System;

namespace Quantum
{
	[Serializable]
	public struct QuantumPair<T1, T2>
	{
		public T1 Value1;
		public T2 Value2;

		public QuantumPair(T1 value1, T2 value2)
		{
			Value1 = value1;
			Value2 = value2;
		}

		public override string ToString()
		{
			return $"[{Value1.ToString()},{Value2.ToString()}]";
		}

		public void Deconstruct(out T1 value1, out T2 value2)
		{
			value1 = Value1;
			value2 = Value2;
		}
	}
}