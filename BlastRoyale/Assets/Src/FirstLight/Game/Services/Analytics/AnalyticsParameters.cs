namespace FirstLight.Game.Services.Analytics
{
	/// <summary>
	/// All (unique and typed) parameters that can be used in analytics events.
	/// </summary>
	public static class AnalyticsParameters
	{
		#region Tutorial

		public const string TUTORIAL_STEP_NAME = "tutorial_step_name"; // string
		public const string TUTORIAL_STEP_NUMBER = "tutorial_step_number"; // int

		#endregion

		#region Match

		public const string MATCH_ID = "match_id"; // string
		public const string MATCH_TYPE = "match_type"; // string
		public const string GAME_MODE = "game_mode"; // string
		public const string MUTATORS = "mutators"; // string
		public const string TOTAL_PLAYERS = "total_players"; // int
		public const string MAP_ID = "map_id"; // string
		public const string TEAM_SIZE = "team_size"; // int
		public const string ITEM_TYPE = "item_type"; // string
		public const string KILLS = "kills"; // int
		public const string MATCH_TIME = "match_time"; // float
		public const string MATCH_END_STATE = "match_end_state"; // float
		public const string PLAYER_RANK = "player_rank"; // int
		public const string PLAYER_ATTACKS = "player_attacks"; // int

		#endregion

		#region Meta

		public const string SCREEN_NAME = "screen_name"; // string
		public const string BUTTON_NAME = "button_name"; // string
		public const string BP_SEASON = "bp_season"; // int
		public const string BP_LEVEL = "bp_level"; // int

		#endregion
	}
}