using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using PlayFab.ClientModels;

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
	}

	public class LeaderboardsService : ILeaderboardService
	{
		public const int MAX_ENTRIES = 100;
		public const string LeaderboardConfigsDataName = "LeaderboardConfigs";
		
		private IGameServices _services;
		private readonly List<GameLeaderboard> _leaderboards = new();
		private LeaderboardConfigs _configs;
		
		public LeaderboardsService(IGameServices services)
		{
			_services = services;
			// TODO: Fix localization _leaderboards.Add(new GameLeaderboard("UITLeaderboards/trophies", "Trophies Ladder", "trophies-icon"));
			_leaderboards.Add(new GameLeaderboard("Ranked", "Trophies Ladder", "trophies-icon"));
			_leaderboards.Add(new GameLeaderboard("Kills", "Kills", "kills-icon"));
			_leaderboards.Add(new GameLeaderboard("Wins", "Games Won", "wins-icon"));
			_leaderboards.Add(new GameLeaderboard("Games", "Games Played", "games-icon"));
			_services.MessageBrokerService.Subscribe<MainMenuOpenedMessage>(OnMenuOpened);
		}

		public LeaderboardConfigs GetConfigs() => _configs;
		
		private void OnMenuOpened(MainMenuOpenedMessage msg)
		{
			if (_configs != null) return;
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
			_services.MessageBrokerService.Unsubscribe<MainMenuOpenedMessage>(OnMenuOpened);
		}

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
		
		private void LeaderboardError(PlayFabError error)
		{
			_services.GameBackendService.HandleError(error, null, AnalyticsCallsErrors.ErrorType.Session);
		}
	}
}