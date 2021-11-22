using System.Collections.Generic;

namespace Quantum
{
	public enum IndicatorVfxId
	{
		None,
		Movement,
		ScalableLine,
		Line,
		Cone,
		Range,
		Radial,
		TOTAL,            // Used to know the total amount of this type without the need of reflection
	}
	
	/// <summary>
	/// Avoids boxing for Dictionary
	/// </summary>
	public class IndicatorVfxIdComparer : IEqualityComparer<IndicatorVfxId>
	{
		public bool Equals(IndicatorVfxId x, IndicatorVfxId y)
		{
			return x == y;
		}

		public int GetHashCode(IndicatorVfxId obj)
		{
			return (int)obj;
		}
	}
}