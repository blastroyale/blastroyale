using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using JetBrains.Annotations;
using PlayFab;
using PlayFab.ClientModels;
using UnityEditor.Graphs;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public interface ILeaderboardService
	{
		/// <summary>
		/// Gets all currently registered leaderboards.
		/// This register happens in-code in service constructor
		/// </summary>
		IReadOnlyList<GameLeaderboard> Leaderboards { get; }

		/// <summary>
		/// Returns the leaderboard config for the given leaderboard
		/// </summary>
		LeaderboardConfigs GetConfigs();

		/// <summary>
		/// Max amount of returned entries leaderboards
		/// </summary>
		int MaxEntries { get; }
		
		/// <summary>
		/// Gets the top ranks of a given leaderboard.
		/// Pagination not yet supported.
		/// </summary>
		void GetTopRankLeaderboard(string metricName, Action<GetLeaderboardResult> onSuccess);

		/// <summary>
		/// Gets neighboards of a given player in leaderboard.
		/// </summary>
		void GetNeighborRankLeaderboard(string metricName, Action<GetLeaderboardAroundPlayerResult> onSuccess);

		/// <summary>
		/// Updates the local player ranking. Will read ranking from playfab and update local data.
		/// </summary>
		PlayerLeaderboardEntry CurrentRankedEntry { get; }

		/// <summary>
		/// Gets the color for a given rank for a given leaderboard
		/// </summary>
		Color GetRankColor(GameLeaderboard board, int rank);

		/// <summary>
		/// Gets the main ranked board
		/// </summary>
		public GameLeaderboard Ranked { get; }
	}

	public class LeaderboardsService : ILeaderboardService
	{
		public event Action<PlayerLeaderboardEntry> OnRankingUpdate;
		
		public const int MAX_ENTRIES = 100;
		public const string LeaderboardConfigsDataName = "LeaderboardConfigs";
		private readonly Color GOLD = Color.yellow;
		private readonly Color SILVER = new (163/255f, 163/255f, 194/255f);
		private readonly Color BRONZE = new (153/255f, 153/255f, 102/255f); 
		
		private IGameServices _services;
		private readonly List<GameLeaderboard> _leaderboards = new();
		private LeaderboardConfigs _configs;
		private PlayerLeaderboardEntry _currentRankedEntry = new();

		public LeaderboardsService(IGameServices services)
		{
			_services = services;
			// TODO: Fix localization _leaderboards.Add(new GameLeaderboard("UITLeaderboards/trophies", "Trophies Ladder", "trophies-icon"));
			_leaderboards.Add(new GameLeaderboard("Ranked", GameConstants.Stats.LEADERBOARD_LADDER_NAME, "trophies-icon"));
			_leaderboards.Add(new GameLeaderboard("Kills", GameConstants.Stats.KILLS, "kills-icon"));
			_leaderboards.Add(new GameLeaderboard("Wins", GameConstants.Stats.GAMES_WON, "wins-icon"));
			_leaderboards.Add(new GameLeaderboard("Games", GameConstants.Stats.GAMES_PLAYED, "games-icon"));
			_services.MessageBrokerService.Subscribe<MainMenuOpenedMessage>(OnMenuOpened);
		}

		public LeaderboardConfigs GetConfigs() => _configs;
		
		private void OnMenuOpened(MainMenuOpenedMessage msg)
		{
			if (_configs == null)
			{
				FetchLeaderboardConfigs();
			}
			UpdateLocalPlayerClientRank();
		}

		private void FetchLeaderboardConfigs()
		{
			var data = _services.DataService.GetData<AppData>().TitleData;
			if(!data.TryGetValue(LeaderboardConfigsDataName, out var config)) return;
			_configs = ModelSerializer.Deserialize<LeaderboardConfigs>(config);
			foreach (var board in _leaderboards)
			{
				if (!_configs.ContainsKey(board.MetricName))
				{
					throw new Exception($"Could not find leaderboard configs in playfab title data for metric {board.MetricName}");
				}
			}
		}

		public GameLeaderboard Ranked => _leaderboards.First();

		public int MaxEntries => MAX_ENTRIES;
		
		public IReadOnlyList<GameLeaderboard> Leaderboards => _leaderboards;
		
		public void GetTopRankLeaderboard(string metricName, Action<GetLeaderboardResult> onSuccess)
		{
			var leaderboardRequest = new GetLeaderboardRequest()
			{
				StatisticName = metricName, 
				StartPosition = 0, MaxResultsCount = MAX_ENTRIES,
				ProfileConstraints = new PlayerProfileViewConstraints { ShowAvatarUrl = true, ShowDisplayName = true}
			};

			PlayFabClientAPI.GetLeaderboard(leaderboardRequest, onSuccess, LeaderboardError);
		}

		
		public void GetNeighborRankLeaderboard(string metricName, Action<GetLeaderboardAroundPlayerResult> onSuccess)
		{
			var neighborLeaderboardRequest = new GetLeaderboardAroundPlayerRequest()
			{
				StatisticName = metricName, 
				MaxResultsCount = 1
			};
			PlayFabClientAPI.GetLeaderboardAroundPlayer(neighborLeaderboardRequest, onSuccess, LeaderboardError);
		}

		public PlayerLeaderboardEntry CurrentRankedEntry => _currentRankedEntry;
		public Color GetRankColor(GameLeaderboard board, int rank)
		{
			if (board != Ranked || rank <= 0) return default;
			// TODO: Put in configs
			if (rank < 6) return GOLD;
			else if (rank < 21) return SILVER;
			else if (rank < 101) return BRONZE;
			return default;
		}

		public void UpdateLocalPlayerClientRank()
		{
			PlayFabClientAPI.GetLeaderboardAroundPlayer(new ()
			{
				StatisticName = GameConstants.Stats.LEADERBOARD_LADDER_NAME,
				MaxResultsCount = 1,
			}, r =>
			{
				if (r.Leaderboard.Count == 1)
				{
					var newEntry = r.Leaderboard[0];
					if (newEntry.StatValue == 0) return;
					if (_currentRankedEntry == null || _currentRankedEntry.Position != newEntry.Position)
					{
						OnRankingUpdate?.Invoke(newEntry);
					}
					_currentRankedEntry = newEntry;
					_currentRankedEntry.Position += 1;
					FLog.Verbose("Updated leaderboard entry for local player");
				}
			}, LeaderboardError);
		}

		private void LeaderboardError(PlayFabError error)
		{
			_services.GameBackendService.HandleError(error, null, AnalyticsCallsErrors.ErrorType.Session);
		}
	}
}