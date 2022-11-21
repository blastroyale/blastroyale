using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Services
{
	public interface IMatchDataService
	{
		List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; set; }
		
		bool ShowUIStandingsExtraInfo { get; set; }
		
		PlayerRef LocalPlayer { get; set; }

		Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; set; }
	}

	public class PlayerMatchData
	{
		public PlayerRef PlayerRef { get; }
		
		public QuantumPlayerMatchData QuantumPlayerMatchData;
		public Equipment Weapon { get; }
		public List<Equipment> Gear { get; }

		public PlayerMatchData(PlayerRef playerRef, QuantumPlayerMatchData quantumData, Equipment weapon, List<Equipment> gear)
		{
			PlayerRef = playerRef;
			QuantumPlayerMatchData = quantumData;
			Weapon = weapon;
			Gear = gear;
		}
	}
	
	public class MatchDataService : IMatchDataService
	{
		public List<QuantumPlayerMatchData> QuantumPlayerMatchData { get; set; }
		public bool ShowUIStandingsExtraInfo { get; set; }
		public PlayerRef LocalPlayer { get; set; }
		public Dictionary<PlayerRef, PlayerMatchData> PlayerMatchData { get; set; }
	}
}