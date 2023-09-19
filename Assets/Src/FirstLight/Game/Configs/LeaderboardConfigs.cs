using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Utils;
using Newtonsoft.Json;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Represents a leaderboard that is displayed in game.
	/// </summary>
	[Serializable]
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
	
	/// <summary>
	/// Represents a season config for a leaderboard
	/// </summary>
	[Serializable]
	public class SeasonConfig  
	{
		public string Desc;
		public string Rewards;
		public string ManualEndTime;
	}
	
	/// <summary>
	/// Represents a leaderboard config, one for all seasons
	/// 
	/// </summary>
	[Serializable]
	public class LeaderboardConfig : SerializedDictionary<int, SeasonConfig>
	{
		public bool HasSeason(int season) => ContainsKey(season);

		public SeasonConfig GetSeason(int season) => this[season];
		
		public int LastSeason => Keys.Max();

		public SeasonConfig LastSeasonConfig => this[LastSeason];
	}
	
	/// <summary>
	/// Contains all configs for all leaderboards, separated by metric name
	/// </summary>
	[Serializable]
	public class LeaderboardConfigs : SerializedDictionary<string, LeaderboardConfig>
	{
		public IReadOnlyList<string> Metrics => Keys.ToList();

		public LeaderboardConfig GetConfig(GameLeaderboard board) => TryGetValue(board.MetricName, out var cfg) ? cfg : null;
	}
}