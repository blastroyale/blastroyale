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
		public QuantumProperty<List<string>> Mutators;
		public QuantumProperty<bool> HasBots;
		public QuantumProperty<List<GameId>> AllowedRewards;
		public QuantumProperty<bool> GameStarted;
		public QuantumProperty<bool> StartCustomGame;
		public QuantumProperty<int> LoadingStartServerTime;
		public QuantumProperty<int> SecondsToStart;
		public QuantumProperty<int> BotDifficultyOverwrite;
		public QuantumProperty<int> TeamSize;

		// For matchmaking, rooms are segregated by casual/ranked.
		public QuantumProperty<MatchType> MatchType;

		// For matchmaking, rooms are segregated by casual/ranked.
		public QuantumProperty<string> GameModeId;

		// Set the game map Id for the same matchmaking
		public QuantumProperty<GameId> MapId;

		public RoomProperties()
		{
			// keys here do not matter, it should be short and unique.
			// all properties should be access through this object
			Commit = Create<string>("cm", true);
			Mutators = CreateList("mttrs", true);
			MatchType = CreateEnum<MatchType>("mt", true);
			AllowedRewards = CreateEnumList<GameId>("alrewards", true);
			GameModeId = Create<string>("gmid", true);
			MapId = CreateEnum<GameId>("mapid", true);
			GameStarted = Create<bool>("started", true);
			HasBots = Create<bool>("bots");
			SecondsToStart = Create<int>("secondstostart");
			LoadingStartServerTime = Create<int>("loading");
			BotDifficultyOverwrite = Create<int>("botdif");
			StartCustomGame = Create<bool>("cstart");
			TeamSize = Create<int>("TeamSize");
		}
	}
}