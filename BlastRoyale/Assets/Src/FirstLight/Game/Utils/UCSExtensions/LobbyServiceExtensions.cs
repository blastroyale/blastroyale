using Cysharp.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace FirstLight.Game.Utils.UCSExtensions
{
	/// <summary>
	/// Helpers for the UCS lobby service.
	/// </summary>
	public static class LobbyServiceExtensions
	{
		/// <summary>
		/// Returns the current squad lobby, or null if player isn't in a squad.
		/// </summary>
		public static async UniTask<Lobby> GetCurrentSquadLobby(this ILobbyService lobbyService)
		{
			var joinedLobbies = await lobbyService.GetJoinedLobbiesAsync();

			foreach (var joinedLobby in joinedLobbies)
			{
				var lobby = await lobbyService.GetLobbyAsync(joinedLobby);
				if (lobby.IsPrivate) // Private lobbies are always squad lobbies
				{
					return lobby;
				}
			}

			return null;
		}
	}
}