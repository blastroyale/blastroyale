using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial class EventOnLocalPlayerLeft
	{
		public QuantumPlayerMatchData MatchData;
	}
	
	public unsafe partial class EventOnPlayerKilledPlayer
	{
		public QuantumPlayerMatchData DeadMatchData;
		public QuantumPlayerMatchData KillerMatchData;
		public QuantumPlayerMatchData[] PlayersMatchData;
	}
	
	public partial class Frame 
	{
		public unsafe partial struct FrameEvents 
		{
			public void OnLocalPlayerLeft(PlayerRef Player)
			{
				var matchData = _f.GetSingleton<GameContainer>().PlayersData[Player];
				
				if (_f.Has<BotCharacter>(matchData.Entity))
				{
					return;
				}
				
				var ev = OnLocalPlayerLeft(Player, matchData.Entity);

				if (ev != null)
				{
					ev.MatchData = new QuantumPlayerMatchData(_f, matchData);
				}
			}
			
			public void OnPlayerKilledPlayer(PlayerRef PlayerDead, PlayerRef PlayerKiller)
			{
				var container = _f.GetSingleton<GameContainer>();
				var data = container.PlayersData;
				var ev = OnPlayerKilledPlayer(PlayerDead, data[PlayerDead].Entity, 
				                              PlayerKiller, data[PlayerKiller].Entity);

				if (ev == null)
				{
					return;
				}
				
				ev.PlayersMatchData = container.GetPlayersMatchData(_f);
				ev.DeadMatchData = ev.PlayersMatchData[PlayerDead];
				ev.KillerMatchData = ev.PlayersMatchData[PlayerKiller];
			}
		}
	}
}