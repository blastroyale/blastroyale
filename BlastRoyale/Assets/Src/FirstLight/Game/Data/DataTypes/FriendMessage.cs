using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Represents a message that was sent by a player.
	/// Used by the Friends API.
	/// </summary>
	[Preserve]
	[DataContract]
	public class FriendMessage
	{
		/// <summary>
		/// What kind of message is this.
		/// </summary>
		[Preserve]
		[DataMember(Name = "messageType", IsRequired = false, EmitDefaultValue = true)]
		public FriendMessageType MessageType { get; private set; }

		/// <summary>
		/// The ID of the lobby to join.
		/// </summary>
		[Preserve]
		[DataMember(Name = "lobby_id", IsRequired = false, EmitDefaultValue = true)]
		public string LobbyID { get; private set; }

		public static FriendMessage CreatePartyInvite(string partyLobbyID)
		{
			return new FriendMessage
			{
				MessageType = FriendMessageType.PartyInvite,
				LobbyID = partyLobbyID
			};
		}

		public static FriendMessage CreateMatchInvite(string matchLobbyID)
		{
			return new FriendMessage
			{
				MessageType = FriendMessageType.MatchInvite,
				LobbyID = matchLobbyID
			};
		}

		public enum FriendMessageType
		{
			PartyInvite,
			MatchInvite
		}
	}
}