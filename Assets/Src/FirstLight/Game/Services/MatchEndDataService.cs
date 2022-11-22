using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service that holds all the data from the match to be used once the simulation is over. It's only updated when the game is over.
	/// </summary>
	public interface IMatchEndDataService
	{
		// TODO: Remove this property once all the match end screens are redone and use PlayerMatchData instead
		/// <summary>
		/// List of all the QuantumPlayerData at the end of the game. Used in the places that need the frame.GetSingleton<GameContainer>().GetPlayersMatchData
		/// </summary>
		List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; }
		
		/// <summary>
		/// Config value used to know if the match end leaderboard should show the extra info
		/// </summary>
		bool ShowUIStandingsExtraInfo { get; }
		
		/// <summary>
		/// LocalPlayer at the end of the game. Will be PlayerRef.None if we're spectators
		/// </summary>
		PlayerRef LocalPlayer { get; }

		/// <summary>
		/// Information about all the players that played in the match that ended.
		/// </summary>
		Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; }
	}

	public struct PlayerMatchData
	{
		public PlayerRef PlayerRef { get; }
		
		public QuantumPlayerMatchData QuantumPlayerMatchData;
		public Equipment Weapon { get; }
		public List<Equipment> Gear { get; }

		public PlayerMatchData(PlayerRef playerRef, QuantumPlayerMatchData quantumData, Equipment weapon, List<Equipment> gear)
		{
			PlayerRef = playerRef;
			QuantumPlayerMatchData = quantumData;
			Weapon = weapon;
			Gear = gear;
		}
	}
	
	/// <inheritdoc />
	public class MatchEndDataService : IMatchEndDataService
	{
		/// <inheritdoc />
		public List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; set; }
		/// <inheritdoc />
		public bool ShowUIStandingsExtraInfo { get; set; }
		/// <inheritdoc />
		public PlayerRef LocalPlayer { get; set; }
		/// <inheritdoc />
		public Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; set; }

		public MatchEndDataService(QuantumGame game)
		{
			FetchEndOfMatchData(game);
		}

		private void FetchEndOfMatchData(QuantumGame  game)
		{
			var frame = game.Frames.Verified;
			var quantumPlayerMatchData = frame.GetSingleton<GameContainer>().GetPlayersMatchData(frame, out _);

			QuantumPlayerMatchData = quantumPlayerMatchData;

			PlayerMatchData = new Dictionary<PlayerRef, PlayerMatchData>();
			
			foreach (var quantumPlayerData in quantumPlayerMatchData)
			{
				Equipment weapon = default;
				List<Equipment> loadout = null;

				var playerRuntimeData = frame.GetPlayerData(quantumPlayerData.Data.Player);
				if (playerRuntimeData != null)
				{
					weapon = playerRuntimeData.Weapon;
					loadout = playerRuntimeData.Loadout.ToList();
				}

				var playerData = new PlayerMatchData(quantumPlayerData.Data.Player, quantumPlayerData, weapon, loadout??new List<Equipment>());
				PlayerMatchData.Add(playerData.PlayerRef, playerData);
			}

			ShowUIStandingsExtraInfo =
				frame.Context.GameModeConfig.ShowUIStandingsExtraInfo;
			LocalPlayer = game.GetLocalPlayerRef();
		}
	}
}