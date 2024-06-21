using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Data
{
	public class CustomMatchSettings
	{
		public string GameModeID = "BattleRoyale";
		public int SquadSize = 1;
		public string MapID = "MazeMayhem";
		public int MaxPlayers = 48;
		public Mutator Mutators = new ();
		public List<string> WeaponFilter = new ();
		public bool PrivateRoom = false;
		public bool ShowCreatorName = true;
	}
}