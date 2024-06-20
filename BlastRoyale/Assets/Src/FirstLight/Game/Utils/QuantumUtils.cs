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
		public unsafe static List<QuantumPlayerMatchData> GeneratePlayersMatchDataLocal(
			this QuantumGame game, out PlayerRef leader, out bool localWinner)
		{
			var f = game.Frames.Verified;
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();

			var matchData = container->GeneratePlayersMatchData(f, out leader, out var leaderTeam);

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
		public unsafe static bool IsGameOver(this QuantumGame game)
		{
			var f = game.Frames.Verified;
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			return container->IsGameOver;
		}

		public static bool IsLocalPlayerNotPresent()
		{
			if (QuantumRunner.Default != null && QuantumRunner.Default.Game != null)
			{
				var game = QuantumRunner.Default.Game;
				var entity = game.GetLocalPlayerEntityRef();
				if (!entity.IsValid || !game.Frames.Verified.Exists(entity)) return false;
				return !game.Frames.Verified.Get<PlayerCharacter>(entity).RealPlayer;
			}

			return false;
		}
	}
}