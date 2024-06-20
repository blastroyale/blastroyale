namespace FirstLight.Game.Services.Analytics.Events
{
	/// <summary>
	/// Triggered when a match is started
	/// TODO: Move to server
	/// </summary>
	public class MatchStartEvent : FLEvent
	{
		public MatchStartEvent(string matchId, string matchType, string gameModeID, string mutators, int totalPlayers, string mapID, int teamSize) :
			base("match_start")
		{
			SetParameter(AnalyticsParameters.MATCH_ID, matchId);
			SetParameter(AnalyticsParameters.MATCH_TYPE, matchType);
			SetParameter(AnalyticsParameters.GAME_MODE, gameModeID);
			SetParameter(AnalyticsParameters.MUTATORS, mutators);
			SetParameter(AnalyticsParameters.TOTAL_PLAYERS, totalPlayers);
			SetParameter(AnalyticsParameters.MAP_ID, mapID);
			SetParameter(AnalyticsParameters.TEAM_SIZE, teamSize);
		}
	}
}