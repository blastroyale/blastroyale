using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using PlayFab.ClientModels;
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
		LeaderboardConfig GetConfigs();

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

		/// <summary>
		/// Event happens when player's rank in the global leaderboard changes
		/// </summary>
		event Action<PlayerLeaderboardEntry> OnRankingUpdate;
	}

	public class LeaderboardsService : ILeaderboardService
	{
		public event Action<PlayerLeaderboardEntry> OnRankingUpdate;

		public const int MAX_ENTRIES = 100;
		public const string LeaderboardConfigsDataName = "LeaderboardConfigs";

		private IGameServices _services;
		private readonly List<GameLeaderboard> _leaderboards = new ();
		private PlayerLeaderboardEntry _currentRankedEntry = new ();
		private Dictionary<string, int> _currentSeasons = new ();

		public LeaderboardsService(IGameServices services)
		{
			_services = services;
			_services.MessageBrokerService.Subscribe<MainMenuLoadedMessage>(OnMenuOpened);
			_services.MessageBrokerService.Subscribe<SuccessfullyAuthenticated>(OnAuthenticated);
		}

		private void OnAuthenticated(SuccessfullyAuthenticated obj)
		{
			if (obj.PreviouslyLoggedIn) return;
			FetchLeaderboardConfigs();
		}

		public LeaderboardConfig GetConfigs() => MainInstaller.ResolveData().RemoteConfigProvider.GetConfig<LeaderboardConfig>();

		private void OnMenuOpened(MainMenuLoadedMessage msg)
		{
			if (_leaderboards.Count == 0)
			{
				FetchLeaderboardConfigs();
			}

			UpdateLocalPlayerClientRank();
		}

		private void FetchLeaderboardConfigs()
		{
			foreach (var (metricName, config) in GetConfigs())
			{
				if (!_currentSeasons.TryGetValue(metricName, out var currentSeason)) currentSeason = config.LastSeason;
				var seasonConfig = config.GetSeason(currentSeason);
				if (seasonConfig.Visible)
				{
					var lb = new GameLeaderboard(seasonConfig.Name, metricName);
					_leaderboards.Add(lb);
					if (metricName == GameConstants.Stats.RANKED_LEADERBOARD_LADDER_NAME) // likely never change so hard coded
					{
						Ranked = lb;
					}

					FLog.Verbose($"Registered Leaderboard for metric {metricName}");
				}
			}
		}

		public GameLeaderboard Ranked { get; set; }

		public int MaxEntries => MAX_ENTRIES;

		public IReadOnlyList<GameLeaderboard> Leaderboards => _leaderboards;

		public void GetTopRankLeaderboard(string metricName, Action<GetLeaderboardResult> onSuccess)
		{
			var leaderboardRequest = new GetLeaderboardRequest()
			{
				StatisticName = metricName,
				StartPosition = 0, MaxResultsCount = MAX_ENTRIES,
				ProfileConstraints = new PlayerProfileViewConstraints {ShowAvatarUrl = true, ShowDisplayName = true}
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
			if (board != Ranked || rank <= 0) return GameConstants.PlayerName.DEFAULT_COLOR;

			if (rank <= GameConstants.Data.LEADERBOARD_GOLD_ENTRIES) return GameConstants.PlayerName.GOLD_COLOR;
			else if (rank <= GameConstants.Data.LEADERBOARD_SILVER_ENTRIES) return GameConstants.PlayerName.SILVER_COLOR;
			else if (rank <= GameConstants.Data.LEADERBOARD_BRONZE_ENTRIES) return GameConstants.PlayerName.BRONZE_COLOR;
			return GameConstants.PlayerName.DEFAULT_COLOR;
		}

		public void UpdateLocalPlayerClientRank()
		{
			PlayFabClientAPI.GetLeaderboardAroundPlayer(new ()
			{
				StatisticName = GameConstants.Stats.RANKED_LEADERBOARD_LADDER_NAME,
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
			_services.GameBackendService.HandleError(error, null);
		}

		private void OnLogin(LoginResult login)
		{
			_currentSeasons = login.InfoResultPayload.PlayerStatistics.ToDictionary(k => k.StatisticName, k => (int) k.Version);
		}
	}
}