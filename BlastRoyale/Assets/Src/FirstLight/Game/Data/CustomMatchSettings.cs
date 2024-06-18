using System.Collections.Generic;

namespace FirstLight.Game.Data
{
	public class CustomMatchSettings
	{
		public string GameModeID = "BattleRoyale";
		public int SquadSize = 1;
		public string MapID = "MazeMayhem";
		public int MaxPlayers = 48;
		public List<string> Mutators = new ();
		public bool PrivateRoom = false;
		public bool ShowCreatorName = true;
	}
}