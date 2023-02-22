using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial class EventOnGameEnded
	{
		public List<QuantumPlayerMatchData> PlayersMatchData;
	}
	
	public unsafe partial class EventOnAllPlayersJoined
	{
		public List<QuantumPlayerMatchData> PlayersMatchData;
	}
	
	public partial class Frame 
	{
		public unsafe partial struct FrameEvents
		{
			public void OnAllPlayersJoined()
			{
				var container = _f.GetSingleton<GameContainer>();
				var matchData = container.GeneratePlayersMatchData(_f, out var leader);
				var ev = OnAllPlayersJoined((uint) matchData.Count);

				if (ev == null)
				{
					return;
				}

				ev.PlayersMatchData = matchData;
			}
			public void OnGameEnded()
			{
				var container = _f.GetSingleton<GameContainer>();
				var data = container.PlayersData;
				var matchData = container.GeneratePlayersMatchData(_f, out var leader);
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