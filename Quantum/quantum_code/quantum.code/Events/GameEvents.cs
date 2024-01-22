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
				var container = _f.Unsafe.GetPointerSingleton<GameContainer>();
				var matchData = container->GeneratePlayersMatchData(_f, out _, out _);
				var ev = OnAllPlayersJoined((uint) matchData.Count);

				if (ev == null)
				{
					return;
				}

				ev.PlayersMatchData = matchData;
			}
			public void OnGameEnded()
			{
				var container = _f.Unsafe.GetPointerSingleton<GameContainer>();
				var data = container->PlayersData;
				var matchData = container->GeneratePlayersMatchData(_f, out var leader, out var leaderTeam);
				if (!leader.IsValid || data.Length == 0 || leader >= data.Length)
				{
					Log.Error($"Could not call game end event: Leader: {leader} match data list size: {data.Length}");
					return;
				}
				var ev = OnGameEnded(leader, data[leader].Entity, leaderTeam);
				if (ev == null)
				{
					return;
				}

				ev.PlayersMatchData = matchData;
			}
		}
	}
}