using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Utils / extensions related to quantum types and logic.
	/// </summary>
	public static class QuantumUtils
	{
		/// <summary>
		/// In addition to giving the results of <see cref="GameContainer.GeneratePlayersMatchData"/> this also
		/// determined if the local player should be considered a winner (e.g. is part of the winning team).
		/// </summary>
		public static List<QuantumPlayerMatchData> GeneratePlayersMatchDataLocal(
			this QuantumGame game, out PlayerRef leader, out bool localWinner)
		{
			var f = game.Frames.Verified;
			var container = f.GetSingleton<GameContainer>();

			var matchData = container.GeneratePlayersMatchData(f, out leader, out var leaderTeam);

			localWinner = game.PlayerIsLocal(leader) || matchData[game.GetLocalPlayerRef()].TeamId == leaderTeam;

			return matchData;
		}
	}
}