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
		public QuantumPlayerMatchData KillerData;
	}
	
	public unsafe partial class EventOnPlayerKilledPlayer
	{
		public PlayerRef PlayerLeader;
		public EntityRef EntityLeader;
		public QuantumPlayerMatchData[] PlayersMatchData;
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
			
			public void OnLocalPlayerDead(PlayerRef Player, PlayerRef killer)
			{
				var data = _f.GetSingleton<GameContainer>().PlayersData;
				var matchData = data[Player];
				var killerData = data[killer];
				
				var ev = OnLocalPlayerDead(Player, matchData.Entity, killer, killerData.Entity);

				if (ev == null)
				{
					return;
				}

				ev.PlayerData = new QuantumPlayerMatchData(_f, matchData);
				ev.KillerData = new QuantumPlayerMatchData(_f, killerData);
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
				
				ev.PlayersMatchData = container.GetPlayersMatchData(_f, out var leader);
				ev.PlayerLeader = leader;
				ev.EntityLeader = data[leader].Entity;
			}
		}
	}
}