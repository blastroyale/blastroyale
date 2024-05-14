using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.MultiplayerModels;

namespace FirstLight.Game.Services.Party
{
	public class PartyMember
	{
		public const string DISPLAY_NAME_MEMBER_PROPERTY = "display_name";
		public const string CHARACTER_SKIN_PROPERTY = "character_skin";
		public const string MELEE_SKIN_PROPERTY = "melee_skin";
		public const string READY_MEMBER_PROPERTY = "ready";
		public const string TROPHIES_PROPERTY = "trophies";
		public const string PROFILE_MASTER_ID = "profile_master_id";

		private string _playfabId;
		private bool _leader;

		public string PlayfabID
		{
			get => _playfabId;
			set => _playfabId = value;
		}

		public bool Leader
		{
			get => _leader;
			set => _leader = value;
		}

		private string EntityType = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE;

		public string DisplayName => RawProperties[DISPLAY_NAME_MEMBER_PROPERTY];
		public string CharacterSkin => RawProperties[CHARACTER_SKIN_PROPERTY];
		public string MeleeSkin => RawProperties[MELEE_SKIN_PROPERTY];
		public string ReadyVersion => RawProperties[READY_MEMBER_PROPERTY];
		public string Trophies => RawProperties[TROPHIES_PROPERTY];
		public string ProfileMasterId => RawProperties[PROFILE_MASTER_ID];

		public bool Local => PlayFabSettings.staticPlayer.EntityId == _playfabId;

		public Dictionary<string, string> RawProperties;

		protected bool Equals(PartyMember other)
		{
			return RawProperties.Count == other.RawProperties.Count && !RawProperties.Except(other.RawProperties).Any() && PlayfabID == other.PlayfabID && DisplayName == other.DisplayName && Leader == other.Leader;
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
			return HashCode.Combine(RawProperties, PlayfabID, DisplayName, Leader);
		}

		public override string ToString()
		{
			return $"PartyMember({nameof(PlayfabID)}: {PlayfabID}, {nameof(DisplayName)}: {DisplayName}, {nameof(Leader)}: {Leader}, {nameof(Local)}: {Local}";
		}

		public EntityKey ToEntityKey()
		{
			return new EntityKey() {Type = EntityType, Id = PlayfabID};
		}
	}
}