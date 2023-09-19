using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Utils;
using Newtonsoft.Json;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Represents a leaderboard that is displayed in-game.
	/// </summary>
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

		public GameLeaderboard(string name, string metric)
		{
			Name = name;
			MetricName = metric;
		}
	}
	
	/// <summary>
	/// Represents a season config for a leaderboard
	/// </summary>
	[Serializable]
	public class SeasonConfig
	{
		public string Name;
		public string Icon = "games-icon";
		public bool Visible = true;
		public string Desc;
		public string Rewards;
		public string ManualEndTime;
	}
	
	/// <summary>
	/// Represents a leaderboard config, one for all seasons
	/// <season int, Season Config>
	/// </summary>
	[Serializable]
	public class LeaderboardConfig : SerializedDictionary<int, SeasonConfig>
	{
		public bool HasSeason(int season) => ContainsKey(season);

		/// <summary>
		/// Will obtain the season config for the given season.
		/// If the season is not present, it will return the last season config
		/// </summary>
		public SeasonConfig GetSeason(int season)
		{
			if (!TryGetValue(season, out var cfg)) return LastSeasonConfig;
			return cfg;
		}

		public int LastSeason => Keys.Max();

		public SeasonConfig LastSeasonConfig => this[LastSeason];
	}
	
	/// <summary>
	/// Contains all configs for all leaderboards, separated by metric name.
	/// <metric name string, Config for Metric>
	/// </summary>
	[Serializable]
	public class LeaderboardConfigs : SerializedDictionary<string, LeaderboardConfig>
	{
		public IReadOnlyList<string> Metrics => Keys.ToList();

		public LeaderboardConfig GetConfig(GameLeaderboard board) => TryGetValue(board.MetricName, out var cfg) ? cfg : null;
	}
}