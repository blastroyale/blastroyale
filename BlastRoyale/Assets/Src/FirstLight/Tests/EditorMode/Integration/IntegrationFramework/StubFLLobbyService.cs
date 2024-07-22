using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using PlayFab.MultiplayerModels;
using Unity.Services.Lobbies.Models;
using Lobby = Unity.Services.Lobbies.Models.Lobby;

namespace FirstLight.Tests.EditorMode
{
	public class StubFLLobbyService : IFLLobbyService
	{
		public Lobby CurrentPartyLobby { get; }
		public FLLobbyEventCallbacks CurrentPartyCallbacks => new ();
		public IReadOnlyList<string> SentPartyInvites { get; }
		public Lobby CurrentMatchLobby { get; }
		public FLLobbyEventCallbacks CurrentMatchCallbacks => new ();
		public IReadOnlyList<string> SentMatchInvites { get; }

		public UniTask CreateParty()
		{
			throw new System.NotImplementedException();
		}

		public UniTask JoinParty(string code)
		{
			throw new System.NotImplementedException();
		}

		public UniTask InviteToParty(string playerID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask LeaveParty()
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> KickPlayerFromParty(string playerID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask TogglePartyReady()
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> UpdatePartyMatchmakingTicket(JoinedMatchmaking ticket)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> UpdatePartyMatchmakingGameMode(string modeID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<List<Lobby>> GetPublicMatches(bool allRegions = false)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> CreateMatch(CustomMatchSettings matchOptions)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> JoinMatch(string lobbyIDOrCode)
		{
			throw new System.NotImplementedException();
		}

		public UniTask InviteToMatch(string playerID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask LeaveMatch()
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> UpdateMatchLobby(CustomMatchSettings settings, bool locked = false)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> SetMatchRoom(string roomName)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> UpdateMatchHost(string playerID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> UpdatePartyHost(string playerID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<bool> KickPlayerFromMatch(string playerID)
		{
			throw new System.NotImplementedException();
		}

		public UniTask SetMatchSpectator(bool spectating)
		{
			throw new System.NotImplementedException();
		}
	}
}