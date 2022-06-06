using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial class EventOnLocalPlayerLeft
	{
		public QuantumPlayerMatchData PlayerData;
	}
	public unsafe partial class EventOnLocalPlayerDead
	{
		public QuantumPlayerMatchData PlayerData;
	}
	
	public unsafe partial class EventOnPlayerKilledPlayer
	{
		public List<QuantumPlayerMatchData> PlayersMatchData;
	}
	
	public partial class Frame 
	{
		public unsafe partial struct FrameEvents 
		{
			public void OnLocalPlayerLeft(PlayerRef Player)
			{
				var matchData = _f.GetSingleton<GameContainer>().PlayersData[Player];
				var ev = OnLocalPlayerLeft(Player, matchData.Entity);

				if (ev == null)
				{
					return;
				}

				ev.PlayerData = new QuantumPlayerMatchData(_f, matchData);
			}
			
			public void OnLocalPlayerDead(PlayerRef Player, PlayerRef killer, EntityRef killerEntity)
			{
				var data = _f.GetSingleton<GameContainer>().PlayersData;
				var matchData = data[Player];
				
				var ev = OnLocalPlayerDead(Player, matchData.Entity, killer, killerEntity);

				if (ev == null)
				{
					return;
				}

				ev.PlayerData = new QuantumPlayerMatchData(_f, matchData);
			}
			
			public void OnPlayerKilledPlayer(PlayerRef PlayerDead, PlayerRef PlayerKiller)
			{
				var container = _f.GetSingleton<GameContainer>();
				var data = container.PlayersData;
				var matchData = container.GetPlayersMatchData(_f, out var leader);
				var ev = OnPlayerKilledPlayer(PlayerDead, data[PlayerDead].Entity, 
				                              PlayerKiller, data[PlayerKiller].Entity, 
				                              leader, data[leader].Entity);

				if (ev == null)
				{
					return;
				}
				
				ev.PlayersMatchData = matchData;
			}
		}
	}
}