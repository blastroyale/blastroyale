using System;
using FirstLight.Game.Services;
using Quantum;
using Unity.Services.Authentication;
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
		public static bool IsPlayerHost(this Lobby lobby)
		{
			return lobby != null && lobby.HostId == AuthenticationService.Instance.PlayerId;
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
	}
}