namespace quantum.custom.plugin
{
	public class FlgConfig
	{
		public static bool TEST_CONSENSUS = false;
		public static readonly double CONSENSUS_PCT = 0.8; // 80% of players
		public static readonly int MIN_PLAYERS_100PCT = 10;
		public static readonly int MIN_PLAYERS = TEST_CONSENSUS ? 1 : 2;
	}
}
