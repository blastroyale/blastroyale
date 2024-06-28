using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Ids;
using Quantum;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.Services.RoomService
{
	public class RoomProperties : PropertiesHolder
	{
		// The commit should guarantee the same Quantum build version + App version etc.
		public QuantumProperty<string> Commit;

		// A list of mutators used in this room
		public QuantumProperty<SimulationMatchConfig> SimulationMatchConfig;
		public QuantumProperty<bool> GameStarted;
		public QuantumProperty<bool> StartCustomGame;
		public QuantumProperty<int> LoadingStartServerTime;
		public QuantumProperty<int> SecondsToStart;

		// For matchmaking, rooms are segregated by casual/ranked.

		public QuantumProperty<Dictionary<string, string>> OverwriteTeams;
		public QuantumProperty<Dictionary<string, string>> TeamMemberColors;
		public QuantumProperty<bool> AutoBalanceTeams;

		public RoomProperties()
		{
			// keys here do not matter, it should be short and unique.
			// all properties should be access through this object
			Commit = Create<string>("cm", true);
			GameStarted = Create<bool>("started", true);
			SecondsToStart = Create<int>("secondstostart");
			LoadingStartServerTime = Create<int>("loading");
			StartCustomGame = Create<bool>("cstart");
			OverwriteTeams = CreateDictionary("overwriteteams");
			TeamMemberColors = CreateDictionary("tmcolors");
			AutoBalanceTeams = Create<bool>("autobalance");
			SimulationMatchConfig = CreateSimulationMatchConfig("mconfig",true);
		}
	}
}