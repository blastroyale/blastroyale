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
		List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; set; }
		
		bool ShowUIStandingsExtraInfo { get; set; }
		
		PlayerRef LocalPlayer { get; set; }

		Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; set; }
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
		public List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; set; }
		public bool ShowUIStandingsExtraInfo { get; set; }
		public PlayerRef LocalPlayer { get; set; }
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