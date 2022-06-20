using System.Collections.Generic;

namespace Quantum
{
	/// <summary>
	/// This class has a list of useful extensions to be used in the quantum project
	/// </summary>
	public  static class QuantumExtensions
	{
		/// <summary>
		/// Sorts a list of player by their <see cref="QuantumPlayerMatchData.PlayerRank"/>
		/// </summary>
		public static void SortByPlayerRank(this List<QuantumPlayerMatchData> players, bool isReverse)
		{
			players.Sort((a, b) =>
			{
				var rank = a.PlayerRank.CompareTo(b.PlayerRank);

				// If players have the same rank, sort them by their PlayerRef index
				if (rank == 0)
				{
					rank = a.Data.Player._index.CompareTo(b.Data.Player._index);
				}

				return isReverse ? rank * -1 : rank;
			});
		}
		
		/// <summary>
		/// Sorts a list of player by their <see cref="QuantumPlayerMatchData.PlayerRank"/>
		/// </summary>
		public static void SortByPlayerRef(this List<QuantumPlayerMatchData> players, bool isReverse)
		{
			players.Sort((a, b) =>
			{
				var rank = a.Data.Player._index.CompareTo(b.Data.Player._index);

				return isReverse ? rank * -1 : rank;
			});
		}
		
		/// <summary>
		/// Returns a mutated <see cref="Equipment"/> at the given <paramref name="level"/>.
		/// If the given <paramref name="level"/> is higher then the equipment <seealso cref="Equipment.MaxLevel"/>, it
		/// will return back the equipment <seealso cref="Equipment.MaxLevel"/>
		/// </summary>
		public static Equipment AtLevel(this Equipment equipment, uint level)
		{
			var ret = equipment;

			ret.Level = level >= ret.MaxLevel ? ret.MaxLevel : level;

			return ret;
		}
	}
}