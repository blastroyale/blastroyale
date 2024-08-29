using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Social;
using Newtonsoft.Json;
using Quantum;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
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
			return JsonConvert.DeserializeObject<CustomMatchSettings>(lobby.Data[FLLobbyService.KEY_LOBBY_MATCH_SETTINGS].Value);
		}

		/// <summary>
		/// Gets the quantum server region for this lobby.
		/// </summary>
		/// <param name="lobby"></param>
		/// <returns></returns>
		public static string GetMatchRegion(this Lobby lobby)
		{
			return lobby.Data[FLLobbyService.KEY_LOBBY_MATCH_REGION].Value;
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
			return player.Data[FLLobbyService.KEY_PLAYER_NAME].Value.TrimPlayerNameNumbers();
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
			var grid = lobby.Data.ContainsKey(FLLobbyService.KEY_LOBBY_MATCH_PLAYER_POSITIONS) ? lobby.GetPlayerGrid() : null;
			return lobby.Players
				.Where(p => !p.IsLocal())
				.Where(p => grid == null || grid.GetPosition(p.Id) != -1) // Host and people not on grid (forced spectators) don't need to be ready
				.All(IsReady);
		}

		public static string GetProperty(this Player player, string name)
		{
			player.Data.TryGetValue(name, out var v);
			return v?.Value;
		}

		/// <summary>
		/// Checks if the player has set themselves ready.
		/// </summary>
		public static bool IsReady(this Player player)
		{
			return player.Data[FLLobbyService.KEY_READY].Value == "true";
		}

		/// <summary>
		/// Returns the playfab id of the player.
		/// </summary>
		public static string GetPlayfabID(this Player player)
		{
			return player.Data[FLLobbyService.KEY_PLAYFAB_ID].Value;
		}

		/// <summary>
		/// Converts the provided Player object into an EntityKey object.
		/// </summary>
		public static PlayFab.MultiplayerModels.EntityKey ToEntityKey(this Player player)
		{
			return new PlayFab.MultiplayerModels.EntityKey
			{
				Id = player.GetPlayfabID(),
				Type = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE
			};
		}

		/// <summary>
		/// Checks if the player is the local player.
		/// </summary>
		public static bool IsLocal(this Player player)
		{
			return player.Id == AuthenticationService.Instance.PlayerId;
		}

		/// <summary>
		/// Checks if the player is a spectator.
		/// </summary>
		public static bool IsSpectator(this Player player)
		{
			return player.Data.TryGetValue(FLLobbyService.KEY_SPECTATOR, out var v) && v.Value == "true";
		}
		
		/// <summary>
		/// Gets non spectators and bots
		/// </summary>
		public static IReadOnlyList<Player> NonSpectators(this Lobby l)
		{
			return l.Players.Where(p => !p.IsSpectator()).ToList();
		}
		
		/// <summary>
		/// Gets non spectators and bots
		/// </summary>
		public static IReadOnlyList<Player> PlayersInGrid(this Lobby l)
		{
			var grid = l.GetPlayerGrid();
			return l.Players.Where(p =>  grid.GetPosition(p.Id) != -1).ToList();
		}
		
		public static Player LocalPlayer(this Lobby l)
		{
			return l.Players.First(p => p.IsLocal());
		}

		public static bool HasRoomInGrid(this Lobby lobby)
		{
			var maxPlayers = lobby.MaxPlayers - GameConstants.Data.MATCH_SPECTATOR_SPOTS;
			return lobby.NonSpectators().Count < maxPlayers;
		}
		
		public static bool HasRoomInSpectators(this Lobby lobby)
		{
			return lobby.Players.Count(p => p.IsSpectator()) < GameConstants.Data.MATCH_SPECTATOR_SPOTS;
		}

		public static string ParseError(this FriendsServiceException e)
		{
			if (e.ErrorCode == FriendsErrorCode.Unknown)
			{
				return string.IsNullOrEmpty(e.Message) ? "list full" : e.Message; // fuck unity
			}

			return e.ErrorCode.ToStringSeparatedWords();
		}

		public static string ParseError(this LobbyServiceException e)
		{
			if (e.Reason == LobbyExceptionReason.UnknownErrorCode)
			{
				return string.IsNullOrEmpty(e.Message) ? "lobby error" : e.Message; // fuck unity more
			}

			return e.Reason.ToStringSeparatedWords();
		}

		/// <summary>
		/// Gets the player grid state of the lobby
		/// </summary>
		public static LobbyGridData GetPlayerGrid(this Lobby lobby)
		{
			return new LobbyGridData(lobby.Data[FLLobbyService.KEY_LOBBY_MATCH_PLAYER_POSITIONS].Value);
		}

		/// <summary>
		/// Gets the index / position of a specific player, or -1 if it can't find it.
		/// </summary>
		public static int GetPlayerPosition(this Lobby lobby, Player player)
		{
			return lobby.GetPlayerGrid().GetPosition(player.Id);
		}

		public static Player GetPlayerByID(this Lobby lobby, string playerID)
		{
			return lobby.Players.FirstOrDefault(p => p.Id == playerID);
		}
	}
}