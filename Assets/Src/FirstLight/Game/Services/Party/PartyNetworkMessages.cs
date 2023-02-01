using System.Collections.Generic;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services.Party
{
	public class LobbyChange
	{
		public uint ChangeNumber;
		public EntityKey memberToMerge;
		public EntityKey memberToDelete;
	}

	public class LobbyPayloadMessage
	{
		public string LobbyId;
		public List<LobbyChange> LobbyChanges;
	}
}