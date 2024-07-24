using System.Net;
using Photon.Realtime;
using FirstLight.SDK.Services;
using Unity.Services.Lobbies;

namespace FirstLight.Game.Messages
{
	/// <summary>
	/// Called when lobby is updated ON CLIENT (so after received and proccessed server updates)
	/// </summary>
	public class PartyLobbyUpdatedMessage : IMessage
	{
		public ILobbyChanges Changes;
	}
	
	public class MatchLobbyUpdatedMessage : IMessage
	{
		public ILobbyChanges Changes;
	}
}