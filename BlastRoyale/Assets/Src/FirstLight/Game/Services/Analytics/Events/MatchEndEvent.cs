namespace FirstLight.Game.Services.Analytics.Events
{
	/// <summary>
	/// Triggered when the match ends.
	/// TODO: Move to server?
	/// </summary>
	public class MatchEndEvent : FLEvent
	{
		public MatchEndEvent(string matchId, string matchType, string gameModeId, string mutators, string mapId, int totalPlayers,
							 int kills, string endState, float matchDuration, int playerRank, int playerNumAttacks) :
			base("match_end")
		{
			SetParameter(AnalyticsParameters.MATCH_ID, matchId);
			SetParameter(AnalyticsParameters.MATCH_TYPE, matchType);
			SetParameter(AnalyticsParameters.GAME_MODE, gameModeId);
			SetParameter(AnalyticsParameters.MUTATORS, mutators);
			SetParameter(AnalyticsParameters.MAP_ID, mapId);
			SetParameter(AnalyticsParameters.TOTAL_PLAYERS, totalPlayers);
			SetParameter(AnalyticsParameters.KILLS, kills);
			SetParameter(AnalyticsParameters.MATCH_TIME, matchDuration);
			SetParameter(AnalyticsParameters.MATCH_END_STATE, endState);
			SetParameter(AnalyticsParameters.PLAYER_RANK, playerRank);
			SetParameter(AnalyticsParameters.PLAYER_ATTACKS, playerNumAttacks);
		}
	}
}