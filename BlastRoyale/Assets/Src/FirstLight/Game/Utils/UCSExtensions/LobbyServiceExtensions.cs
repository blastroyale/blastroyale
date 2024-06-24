using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using Newtonsoft.Json;
using Quantum;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Lobbies.Models;

namespace FirstLight.Game.Utils.UCSExtensions
{
	/// <summary>
	/// Helpers for the UCS lobby service.
	/// </summary>
	public static class LobbyServiceExtensions
	{
		/// <summary>
		/// Checks if the local player is the host of the lobby. If lobby is null, returns false.
		/// </summary>
		public static bool IsLocalPlayerHost(this Lobby lobby)
		{
			return lobby.HostId == AuthenticationService.Instance.PlayerId;
		}

		/// <summary>
		/// Gets the match settings from lobby data.
		/// </summary>
		public static CustomMatchSettings GetMatchSettings(this Lobby lobby)
		{
			return JsonConvert.DeserializeObject<CustomMatchSettings>(lobby.Data[FLLobbyService.KEY_MATCH_SETTINGS].Value);
		}

		/// <summary>
		/// Gets the quantum server region for this lobby.
		/// </summary>
		/// <param name="lobby"></param>
		/// <returns></returns>
		public static string GetMatchRegion(this Lobby lobby)
		{
			return lobby.Data[FLLobbyService.KEY_REGION].Value;
		}

		/// <summary>
		/// Gets the player skin id from the lobby player
		/// </summary>
		public static GameId GetPlayerCharacterSkin(this Player player)
		{
			return Enum.Parse<GameId>(player.Data[FLLobbyService.KEY_SKIN_ID].Value);
		}

		/// <summary>
		/// Gets the player melee id from the lobby player
		/// </summary>
		public static GameId GetPlayerMeleeSkin(this Player player)
		{
			return Enum.Parse<GameId>(player.Data[FLLobbyService.KEY_MELEE_ID].Value);
		}

		/// <summary>
		/// Gets the player name from the lobby player.
		/// TODO: This should be fetched from the Player.Profile but it's always null 
		/// </summary>
		public static string GetPlayerName(this Player player)
		{
			return player.Data[FLLobbyService.KEY_PLAYER_NAME].Value;
		}

		/// <summary>
		/// Returns the number of trophies the player has.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static int GetPlayerTrophies(this Player player)
		{
			// TODO mihak
			return 0;
		}

		/// <summary>
		/// Checks if the player is a friend of the local player.
		/// </summary>
		public static bool IsFriend(this Player player)
		{
			return FriendsService.Instance.GetFriendByID(player.Id) != null;
		}

		/// <summary>
		/// Returns a list of players that are friends with the local player.
		/// </summary>
		public static IEnumerable<Player> GetFriends(this Lobby lobby)
		{
			return lobby.Players.Where(IsFriend);
		}

		/// <summary>
		/// Checks if all players in the lobby are ready.
		/// </summary>
		public static bool IsEveryoneReady(this Lobby lobby)
		{
			return lobby.Players
				.Where(p => p.Id != AuthenticationService.Instance.PlayerId) // Host doesn't need to be ready
				.All(IsReady);
		}

		/// <summary>
		/// Checks if the player has set themselves ready.
		/// </summary>
		public static bool IsReady(this Player player)
		{
			return player.Data[FLLobbyService.KEY_READY].Value == "true";
		}
	}
}