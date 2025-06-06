using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Data
{
	// TODO mihak: Unify this with SimulationMatchConfig
	public class CustomMatchSettings
	{
		public string GameModeID = "BattleRoyale";
		public uint SquadSize = 1;
		public string MapID = "MazeMayhem";
		public int MaxPlayers = 48;
		public int BotDifficulty = 5;
		public Mutator Mutators = new ();
		public List<string> WeaponFilter = new ();
		public bool PrivateRoom = false;
		public bool ShowCreatorName = true;
		public bool RandomizeTeams = false;
		public bool AllowInvites = true;

		public SimulationMatchConfig ToSimulationMatchConfig()
		{
			return new SimulationMatchConfig
			{
				MapId = MapID,
				GameModeID = GameModeID,
				MatchType = MatchType.Custom,
				Mutators = Mutators,
				MaxPlayersOverwrite = MaxPlayers,
				DisableBots = BotDifficulty == 0,
				BotOverwriteDifficulty = BotDifficulty,
				TeamSize = SquadSize,
				WeaponsSelectionOverwrite = WeaponFilter.ToArray()
			};
		}
	}
}