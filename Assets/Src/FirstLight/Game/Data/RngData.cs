using System;
using System.Linq;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Contains all the data in the scope to generate and maintain the random generated values
	/// </summary>
	[Serializable]
	public class RngData
	{
		public int Seed;
		public int Count;
		public int[] State;
		
		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + Seed.GetHashCode();
			hash = hash * 23 + Count.GetHashCode();
			if (State.Length > 0)
			{
				hash = hash * 23 + State.Last().GetHashCode();
			}
			return hash;
		}
	}
}