using System;
using System.Collections.Generic;
using System.Drawing;
using FirstLight.Game.Services.AnalyticsHelpers;
using PlayFab;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services
{

	public class GameLeaderboard
	{
		/// <summary>
		/// Localized name key for the leaderboard
		/// </summary>
		public string Name;
		
		/// <summary>
		/// Should be the same name as the playfab metric name
		/// </summary>
		public string MetricName { get; set; }
		
		/// <summary>
		/// Icon class for the point icon for this leaderboard
		/// </summary>
		public string IconClass;

		public GameLeaderboard(string name, string metric, string iconClass)
		{
			Name = name;
			MetricName = metric;
			IconClass = iconClass;
		}
	}

	public interface ILeaderboardService
	{
		/// <summary>
		/// Gets all currently registered leaderboards.
		/// This register happens in-code in service constructor
		/// </summary>
		IReadOnlyList<GameLeaderboard> Leaderboards { get; }

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
		
		private IGameServices _services;
		private readonly List<GameLeaderboard> _leaderboards = new();
		
		public LeaderboardsService(IGameServices services)
		{
			_services = services;
			// TODO: Fix localization _leaderboards.Add(new GameLeaderboard("UITLeaderboards/trophies", "Trophies Ladder", "trophies-icon"));
			_leaderboards.Add(new GameLeaderboard("Ranked", "Trophies Ladder", "trophies-icon"));
			_leaderboards.Add(new GameLeaderboard("Kills", "Kills", "kills-icon"));
			_leaderboards.Add(new GameLeaderboard("Wins", "Games Won", "wins-icon"));
			_leaderboards.Add(new GameLeaderboard("Games", "Games Played", "games-icon"));
		}
		
		public int MaxEntries => MAX_ENTRIES;
		
		public IReadOnlyList<GameLeaderboard> Leaderboards => _leaderboards;
		
		public void GetTopRankLeaderboard(string metricName, Action<GetLeaderboardResult> onSuccess)
		{
			var leaderboardRequest = new GetLeaderboardRequest()
			{
				StatisticName = metricName, 
				StartPosition = 0, MaxResultsCount = MAX_ENTRIES, 
				ProfileConstraints = new PlayerProfileViewConstraints { ShowAvatarUrl = true, ShowDisplayName = true, }
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