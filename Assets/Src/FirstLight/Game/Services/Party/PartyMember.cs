using System;

namespace FirstLight.Game.Services.Party
{
	public class PartyMember
	{
		public String PlayfabID { get; }
		public String DisplayName { get; }
		public uint BPPLevel { get; }
		public uint Trophies { get; }
		public bool Leader { get; }
		public bool Local { get; }

		public PartyMember(string playfabID, string displayName, uint bppLevel, uint trophies, bool leader, bool local)
		{
			PlayfabID = playfabID;
			DisplayName = displayName;
			BPPLevel = bppLevel;
			Trophies = trophies;
			Leader = leader;
			Local = local;
		}
	}
}