using System;

namespace FirstLight.Game.Services.Party
{
	public class PartyMember
	{
		public string PlayfabID { get; }
		public string DisplayName { get; }
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

		public override string ToString()
		{
			return $"PartyMember({nameof(PlayfabID)}: {PlayfabID}, {nameof(DisplayName)}: {DisplayName}, {nameof(BPPLevel)}: {BPPLevel}, {nameof(Trophies)}: {Trophies}, {nameof(Leader)}: {Leader}, {nameof(Local)}: {Local})";
		}
	}
}