namespace Quantum
{
	public unsafe partial class EventOnGameEnded
	{
		public QuantumPlayerMatchData[] PlayersMatchData;
	}
	
	public partial class Frame 
	{
		public unsafe partial struct FrameEvents
		{
			public void OnGameEnded()
			{
				var container = _f.GetSingleton<GameContainer>();
				var data = container.PlayersData;
				var matchData = container.GetPlayersMatchData(_f, out var leader);
				var ev = OnGameEnded(leader, data[leader].Entity);

				if (ev == null)
				{
					return;
				}

				ev.PlayersMatchData = matchData;
			}
		}
	}
}