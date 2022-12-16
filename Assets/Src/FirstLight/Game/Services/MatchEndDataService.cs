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

		/// <summary>
		/// Has local player left the match before it ended (either through menu UI, or during spectate)
		/// This data point is available before the match ends
		/// </summary>
		public bool LeftBeforeMatchFinished { get; }
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
	public class MatchEndDataService : IMatchEndDataService, MatchServices.IMatchService
	{
		/// <inheritdoc />
		public List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; private set; }
		/// <inheritdoc />
		public bool ShowUIStandingsExtraInfo { get; private set; }
		/// <inheritdoc />
		public PlayerRef LocalPlayer { get; private set; }
		/// <inheritdoc />
		public Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; private set; } = new Dictionary<PlayerRef, PlayerMatchData>();
		/// <inheritdoc />
		public List<RewardData> Rewards { get; private set; }
		/// <inheritdoc />
		public int TrophiesChange { get; private set; }
		/// <inheritdoc />
		public uint TrophiesBeforeChange { get; private set; }
		/// <inheritdoc />
		public uint CSBeforeChange { get; private set; }
		/// <inheritdoc />
		public uint BPPBeforeChange { get; private set; }
		/// <inheritdoc />
		public uint BPLevelBeforeChange { get; private set; }
		/// <inheritdoc />
		public bool LeftBeforeMatchFinished { get; private set; }

		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		public MatchEndDataService(IGameServices services, IGameDataProvider dataProvider)
		{
			_services = services;
			_dataProvider = dataProvider;
			_services.MessageBrokerService.Subscribe<LeftBeforeMatchFinishedMessage>(OnLeftBeforeMatchFinishedMessage);
		}
		
		/// <inheritdoc />
		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			TrophiesBeforeChange = _dataProvider.PlayerDataProvider.Trophies.Value;
			CSBeforeChange = (uint)_dataProvider.CurrencyDataProvider.Currencies[GameId.CS];
			ShowUIStandingsExtraInfo = game.Frames.Verified.Context.GameModeConfig.ShowUIStandingsExtraInfo;
			LocalPlayer = game.GetLocalPlayerRef();
			PlayerMatchData = new Dictionary<PlayerRef, PlayerMatchData>();
		}

		/// <inheritdoc />
		public void OnMatchEnded(QuantumGame game)
		{
			var frame = game.Frames.Verified;
			
			QuantumPlayerMatchData = frame.GetSingleton<GameContainer>().GetPlayersMatchData(frame, out _);

			PlayerMatchData.Clear();
			foreach (var quantumPlayerData in QuantumPlayerMatchData)
			{
				var playerRuntimeData = frame.GetPlayerData(quantumPlayerData.Data.Player);
				var weapon = playerRuntimeData?.Weapon ?? default;
				var loadout = playerRuntimeData?.Loadout.ToList() ?? new List<Equipment>();
				var playerData = new PlayerMatchData(quantumPlayerData.Data.Player, quantumPlayerData, weapon, loadout);
				
				PlayerMatchData.Add(playerData.PlayerRef, playerData);
			}

			GetRewards(frame);
		}
		
		private void OnLeftBeforeMatchFinishedMessage(LeftBeforeMatchFinishedMessage msg)
		{
			LeftBeforeMatchFinished = true;
		}

		private void GetRewards(Frame frame)
		{
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			var matchType = room?.GetMatchType() ?? _services.GameModeService.SelectedGameMode.Value.Entry.MatchType;
			var gameContainer = frame.GetSingleton<GameContainer>();

			if (!frame.Context.GameModeConfig.AllowEarlyRewards && !gameContainer.IsGameCompleted &&
				!gameContainer.IsGameOver)
			{
				return;
			}

			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			BPPBeforeChange = predictedProgress.Item2;
			BPLevelBeforeChange = predictedProgress.Item1;
			
			var rewardSource = new RewardSource()
			{
				MatchData = QuantumPlayerMatchData,
				ExecutingPlayer = LocalPlayer,
				MatchType = matchType,
				DidPlayerQuit = false,
				GamePlayerCount = QuantumPlayerMatchData.Count()
			};
			Rewards = _dataProvider.RewardDataProvider.CalculateMatchRewards(rewardSource, out var trophyChange);
			TrophiesChange = trophyChange;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}
	}
}