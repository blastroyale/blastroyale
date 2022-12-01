using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

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
		
		/// <summary>
		/// List of rewards
		/// </summary>
		public List<RewardData> Rewards { get; }
		
		/// <summary>
		/// How trophies total changed
		/// </summary>
		public int TrophiesChange { get; }
		
		/// <summary>
		/// How many trophies player had before change
		/// </summary>
		public uint TrophiesBeforeChange { get; }
		
		/// <summary>
		/// How much CS the player had before the change
		/// </summary>
		public uint CSBeforeChange { get; }
		
		/// <summary>
		/// How much BPP the player had before the change
		/// </summary>
		public uint BPPBeforeChange { get; }
		/// <summary>
		/// What level was the player in BP before the change
		/// </summary>
		public uint BPLevelBeforeChange { get; }
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
		/// <inheritdoc />
		public List<RewardData> Rewards { get; set; }
		/// <inheritdoc />
		public int TrophiesChange { get; set; }
		/// <inheritdoc />
		public uint TrophiesBeforeChange { get; set; }
		/// <inheritdoc />
		public uint CSBeforeChange { get; set; }
		/// <inheritdoc />
		public uint BPPBeforeChange { get; set; }
		/// <inheritdoc />
		public uint BPLevelBeforeChange { get; set; }

		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		public MatchEndDataService(QuantumGame game, IGameServices services, IGameDataProvider dataProvider)
		{
			_services = services;
			_dataProvider = dataProvider;
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

			GetRewards(game, frame);
		}

		private void GetRewards(QuantumGame  game, Frame frame)
		{
			var executingPlayer = game.GetLocalPlayers()[0];
			var matchType = _services.NetworkService.QuantumClient.CurrentRoom.GetMatchType();

			var gameContainer = frame.GetSingleton<GameContainer>();
			var gameLogic = _services.GameLogic;

			if (!frame.Context.GameModeConfig.AllowEarlyRewards && !gameContainer.IsGameCompleted &&
				!gameContainer.IsGameOver)
			{
				return;
			}
			
			TrophiesBeforeChange = gameLogic.PlayerLogic.Trophies.Value;
			CSBeforeChange = (uint)_dataProvider.CurrencyDataProvider.Currencies[GameId.CS];
			BPPBeforeChange = _dataProvider.BattlePassDataProvider.CurrentPoints.Value;
			BPLevelBeforeChange = _dataProvider.BattlePassDataProvider.CurrentLevel.Value;
			
			var rewardSource = new RewardSource()
			{
				MatchData = QuantumPlayerMatchData,
				ExecutingPlayer = executingPlayer,
				MatchType = matchType,
				DidPlayerQuit = false,
				GamePlayerCount = QuantumPlayerMatchData.Select(d => !d.Data.IsBot).Count()
			};
			Rewards = gameLogic.RewardLogic.GiveMatchRewards(rewardSource, out var trophyChange);
			TrophiesChange = trophyChange;
		}
	}
}