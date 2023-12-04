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

			localWinner = false;

			if (game.PlayerIsLocal(leader))
			{
				localWinner = true;
			}
			else
			{
				var localPlayerRef = game.GetLocalPlayerRef();

				if (localPlayerRef != PlayerRef.None)
				{
					localWinner = matchData[localPlayerRef].TeamId == leaderTeam;
				}
			}

			return matchData;
		}

		/// <summary>
		/// Check if the current game is over
		/// </summary>
		public static bool IsGameOver(this QuantumGame game)
		{
			var f = game.Frames.Verified;
			var container = f.GetSingleton<GameContainer>();

			return container.IsGameOver;
		}

		public static bool HasGameContainer(this QuantumGame game)
		{
			var f = game.Frames.Verified;
			return f.TryGetSingleton<GameContainer>(out _);
		}
	}
}