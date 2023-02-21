using System;
using System.Collections.Generic;
using System.Linq;

namespace FirstLight.Game.Services.Party
{
	public class PartyMember
	{
		public string PlayfabID { get; }
		public string DisplayName { get; set; }
		public uint BPPLevel { get; set; }
		public uint Trophies { get; set; }
		public bool Leader { get; set; }
		public bool Local { get; }
		public bool Ready { get;  set; }

		public Dictionary<string, string> RawProperties;

		public PartyMember(string playfabID, string displayName, uint bppLevel, uint trophies, bool leader, bool local, bool ready, Dictionary<string, string> rawProperties)
		{
			PlayfabID = playfabID;
			DisplayName = displayName;
			BPPLevel = bppLevel;
			Trophies = trophies;
			Leader = leader;
			Local = local;
			Ready = ready;
			RawProperties = rawProperties;
		}

		protected bool Equals(PartyMember other)
		{
			return RawProperties.Count == other.RawProperties.Count && !RawProperties.Except(other.RawProperties).Any() && PlayfabID == other.PlayfabID && DisplayName == other.DisplayName && BPPLevel == other.BPPLevel && Trophies == other.Trophies && Leader == other.Leader && Local == other.Local && Ready == other.Ready;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((PartyMember) obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(RawProperties, PlayfabID, DisplayName, BPPLevel, Trophies, Leader, Local, Ready);
		}

		public override string ToString()
		{
			return $"PartyMember({nameof(PlayfabID)}: {PlayfabID}, {nameof(DisplayName)}: {DisplayName}, {nameof(BPPLevel)}: {BPPLevel}, {nameof(Trophies)}: {Trophies}, {nameof(Leader)}: {Leader}, {nameof(Local)}: {Local}, {nameof(Ready)}: {Ready})";
		}
	}
}