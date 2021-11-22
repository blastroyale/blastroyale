namespace Quantum
{
	public unsafe partial class EventOnGameEnded
	{
		public QuantumPlayerMatchData WinnerMatchData;
	}
	
	public partial class Frame 
	{
		public unsafe partial struct FrameEvents
		{
			public void OnGameEnded(PlayerRef PlayerWinner, PlayerMatchData matchData)
			{
				var ev = OnGameEnded(PlayerWinner);

				if (ev != null)
				{
					ev.WinnerMatchData = new QuantumPlayerMatchData(_f, matchData);
				}
			}
		}
	}
}