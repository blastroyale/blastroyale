﻿using System;
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
		public QuantumProperty<SimulationMatchConfig> SimulationMatchConfig { get; private set; }
		public QuantumProperty<bool> GameStarted{ get; private set; }
		public QuantumProperty<int> LoadingStartServerTime { get; private set; }
		public QuantumProperty<int> SecondsToStart { get; private set; }

		// For matchmaking, rooms are segregated by casual/ranked.


		public RoomProperties()
		{
			// keys here do not matter, it should be short and unique.
			// all properties should be access through this object
			Commit = Create<string>("cm", true);
			GameStarted = Create<bool>("started", true);
			SecondsToStart = Create<int>("secondstostart");
			LoadingStartServerTime = Create<int>("loading");
			SimulationMatchConfig = CreateSimulationMatchConfig("mconfig",true);
		}
	}
}