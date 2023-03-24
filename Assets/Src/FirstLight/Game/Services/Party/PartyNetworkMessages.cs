using System.Collections.Generic;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services.Party
{
	public class LobbyPayloadMessage
	{
		public string lobbyId { get; set; }
		public LobbyChange[] lobbyChanges { get; set; }
	}

	public class LobbyChange
	{
		public uint changeNumber { get; set; }
		public MemberToMerge memberToMerge { get; set; }
		public MemberToDelete memberToDelete { get; set; }
		public EntityKey owner { get; set; }

		public Dictionary<string, string> lobbyData { get; set; }

		public string[] lobbyDataToDelete { get; set; }
	}

	public class MemberToMerge
	{
		public EntityKey memberEntity { get; set; }
		public Dictionary<string, string> memberData { get; set; }
		public bool noPubSubConnectionHandle { get; set; }
	}

	public class MemberToDelete
	{
		public EntityKey memberEntity { get; set; }
	}
}