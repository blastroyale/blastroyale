using System;

namespace Backend.Data
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
	}
}