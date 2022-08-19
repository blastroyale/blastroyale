namespace quantum.custom.plugin
{
	public class FlgConfig
	{
		public static readonly bool TEST_CONSENSUS = true;
		public static readonly double CONSENSUS_PCT = 0.8; // 80% of players
		public static readonly int MIN_PLAYERS_100PCT = 10;
		public static readonly int MIN_PLAYERS = TEST_CONSENSUS ? 1 : 2;
	}
}
