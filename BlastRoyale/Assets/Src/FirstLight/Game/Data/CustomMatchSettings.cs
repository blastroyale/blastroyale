using System;
using System.Collections.Generic;
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
		public int BotDifficulty = 1;
		public Mutator Mutators = new ();
		public List<string> WeaponFilter = new ();
		public bool PrivateRoom = false;
		public bool ShowCreatorName = true;

		public SimulationMatchConfig ToSimulationMatchConfig()
		{
			return new SimulationMatchConfig
			{
				MapId = (int) Enum.Parse<GameId>(MapID),
				GameModeID = GameModeID,
				MatchType = MatchType.Custom,
				Mutators = Mutators,
				MaxPlayersOverwrite = MaxPlayers,
				HasBots = BotDifficulty > 0,
				BotOverwriteDifficulty = BotDifficulty,
				TeamSize = SquadSize,
				WeaponsSelectionOverwrite = WeaponFilter.ToArray()
			};
		}
	}
}