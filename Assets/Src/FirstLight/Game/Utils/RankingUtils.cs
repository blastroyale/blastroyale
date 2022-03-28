using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helper methods related to player ranking / trophies etc...
	/// </summary>
	public static class RankingUtils
	{
		/// <summary>
		/// Sorts a list of player by their <see cref="QuantumPlayerMatchData.PlayerRank"/>
		/// </summary>
		public static void SortByPlayerRank(this List<QuantumPlayerMatchData> players)
		{
			players.Sort((a, b) =>
			{
				var rank = a.PlayerRank.CompareTo(b.PlayerRank);

				// If players have the same rank, sort them by their PlayerRef index
				if (rank == 0)
				{
					return a.Data.Player._index.CompareTo(b.Data.Player._index);
				}

				return rank;
			});
		}
	}
}