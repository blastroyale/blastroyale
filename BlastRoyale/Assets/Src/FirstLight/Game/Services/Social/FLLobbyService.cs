using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.SDK.Services;
using Newtonsoft.Json;
using PlayFab;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.Assertions;

namespace FirstLight.Game.Services
{
	public interface IFLLobbyService
	{
		/// <summary>
		/// The party the player is currently in.
		/// </summary>
		Lobby CurrentPartyLobby { get; }

		/// <summary>
		/// Events that trigger when the party lobby changes.
		/// </summary>
		FLLobbyEventCallbacks CurrentPartyCallbacks { get; }

		/// <summary>
		/// The IDs of the players we sent invites to (only persists for the current session).
		/// </summary>
		IReadOnlyList<string> SentPartyInvites { get; }

		/// <summary>
		/// The custom game the player is currently in.
		/// </summary>
		Lobby CurrentMatchLobby { get; }

		/// <summary>
		/// Events that trigger when the custom game lobby changes.
		/// </summary>
		FLLobbyEventCallbacks CurrentMatchCallbacks { get; }

		/// <summary>
		/// The IDs of the players we sent invites to (only persists for the current session).
		/// </summary>
		IReadOnlyList<string> SentMatchInvites { get; }

		/// <summary>
		/// Creates a new party for the current player with their ID.
		/// </summary>
		UniTask CreateParty();

		/// <summary>
		/// Joins a party with the given code.
		/// </summary>
		UniTask JoinParty(string code);

		/// <summary>
		/// Invites a friend to the current party.
		/// </summary>
		UniTask InviteToParty(string playerID);

		/// <summary>
		/// Leaves the current party.
		/// </summary>
		UniTask LeaveParty();

		/// <summary>
		/// Sets the party host to the given player ID.
		/// </summary>
		UniTask<bool> KickPlayerFromParty(string playerID);

		/// <summary>
		/// Toggles the ready status of the player in the current party.
		/// </summary>
		UniTask TogglePartyReady();

		/// <summary>
		/// Updates the matchmaking ticket for the current party.
		/// </summary>
		UniTask<bool> UpdatePartyMatchmakingTicket(JoinedMatchmaking ticket);

		/// <summary>
		/// Updates the matchmaking ticket for the current party.
		/// </summary>
		UniTask<bool> UpdatePartyMatchmakingGameMode(string modeID);

		/// <summary>
		/// Queries for public lobbies.
		/// </summary>
		UniTask<List<Lobby>> GetPublicMatches(bool allRegions = false);

		/// <summary>
		/// Creates a new public game lobby.
		/// </summary>
		UniTask<bool> CreateMatch(CustomMatchSettings matchOptions);

		/// <summary>
		/// Joins a match lobby by id or code.
		/// </summary>
		UniTask<bool> JoinMatch(string lobbyIDOrCode);

		/// <summary>
		/// Invites a friend to the current party.
		/// </summary>
		UniTask InviteToMatch(string playerID);

		/// <summary>
		/// Leaves the current match labby.
		/// </summary>
		UniTask LeaveMatch();

		/// <summary>
		/// Updates the data / locked state of the current match lobby.
		/// </summary>
		UniTask<bool> UpdateMatchLobby(CustomMatchSettings settings, bool locked = false);

		/// <summary>
		/// Updates the data / locked state of the current match lobby.
		/// </summary>
		UniTask<bool> SetMatchRoom(string roomName);

		/// <summary>
		/// Sets the match host to the given player ID.
		/// </summary>
		UniTask<bool> UpdateMatchHost(string playerID);

		/// <summary>
		/// Sets the party host to the given player ID.
		/// </summary>
		UniTask<bool> UpdatePartyHost(string playerID);

		/// <summary>
		/// Sets the party host to the given player ID.
		/// </summary>
		UniTask<bool> KickPlayerFromMatch(string playerID);

		/// <summary>
		/// Toggles the spectator status for the current player in the match lobby.
		/// </summary>
		UniTask SetMatchSpectator(bool spectating);
	}

	/// <summary>
	/// Handles all lobby-related operations (parties, custom matches).
	/// </summary>
	public class FLLobbyService : IFLLobbyService
	{
		private const string PARTY_LOBBY_NAME = "party_{0}";
		private const string MATCH_LOBBY_NAME = "{0}'s game";
		private const int MAX_PARTY_SIZE = 4;
		private const float TICK_DELAY = 15f;

		public const string KEY_SKIN_ID = "skin_id";
		public const string KEY_MELEE_ID = "melee_id";
		public const string KEY_PLAYER_NAME = "player_name";
		public const string KEY_READY = "ready";
		public const string KEY_PLAYFAB_ID = "playfab_id";
		public const string KEY_SPECTATOR = "spectator";
		public const string KEY_MATCHMAKING_TICKET = "matchmaking_ticket";
		public const string KEY_MATCHMAKING_GAMEMODE = "matchmaking_gamemode";

		public const string KEY_MATCH_SETTINGS = "match_settings";
		public const string KEY_MATCH_ROOM_NAME = "room_name";
		public const string KEY_REGION = "region"; // S1

		/// <summary>
		/// The party the player is currently in.
		/// </summary>
		public Lobby CurrentPartyLobby { get; private set; }

		/// <summary>
		/// Events that trigger when the party lobby changes.
		/// </summary>
		public FLLobbyEventCallbacks CurrentPartyCallbacks { get; } = new ();

		/// <summary>
		/// The IDs of the players we sent invites to (only persists for the current session).
		/// </summary>
		public IReadOnlyList<string> SentPartyInvites => _sentPartyInvites;

		/// <summary>
		/// The custom game the player is currently in.
		/// </summary>
		public Lobby CurrentMatchLobby { get; private set; }

		/// <summary>
		/// Events that trigger when the custom game lobby changes.
		/// </summary>
		public FLLobbyEventCallbacks CurrentMatchCallbacks { get; } = new ();

		/// <summary>
		/// The IDs of the players we sent invites to (only persists for the current session).
		/// </summary>
		public IReadOnlyList<string> SentMatchInvites => _sentMatchInvites;

		private ILobbyEvents _partyLobbyEvents;
		private ILobbyEvents _matchLobbyEvents;

		private readonly IGameDataProvider _dataProvider;
		private readonly NotificationService _notificationService;
		private readonly LocalPrefsService _localPrefsService;
		private readonly List<string> _sentPartyInvites = new ();
		private readonly List<string> _sentMatchInvites = new ();

		public FLLobbyService(IMessageBrokerService messageBrokerService, IGameDataProvider dataProvider, NotificationService notificationService,
							  LocalPrefsService localPrefsService)
		{
			_dataProvider = dataProvider;
			_notificationService = notificationService;
			_localPrefsService = localPrefsService;

			Tick().Forget();

			messageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);
			
			CurrentPartyCallbacks.LobbyChanged += OnPartyLobbyChanged;
			CurrentMatchCallbacks.LobbyChanged += OnMatchLobbyChanged;
			
			((ILobbyServiceSDKConfiguration)LobbyService.Instance).EnableLocalPlayerLobbyEvents(true);
		}
		
		/// <summary>
		/// Creates a new party for the current player with their ID.
		/// </summary>
		public async UniTask CreateParty()
		{
			if (CurrentPartyLobby != null) return;

			var lobbyName = string.Format(PARTY_LOBBY_NAME, AuthenticationService.Instance.PlayerId);
			// TODO: Should not have to resolve services here but there's a circular dependency
			var currentGameMode = MainInstaller.ResolveServices().GameModeService.SelectedGameMode.Value.Entry.MatchConfig.ConfigId;
			var options = new CreateLobbyOptions
			{
				IsPrivate = true,
				Player = CreateLocalPlayer(),
				Data = new ()
				{
					{KEY_MATCHMAKING_GAMEMODE, new DataObject(DataObject.VisibilityOptions.Member, currentGameMode)},
					{KEY_MATCHMAKING_TICKET, new DataObject(DataObject.VisibilityOptions.Member, null)}
				}
			};

			try
			{
				FLog.Info($"Creating new party with name: {lobbyName}");
				CurrentPartyLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PARTY_SIZE, options);
				_partyLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(CurrentPartyLobby.Id, CurrentPartyCallbacks);
				FLog.Info($"Party created! Code: {CurrentPartyLobby.LobbyCode} ID: {CurrentPartyLobby.Id} Name: {CurrentPartyLobby.Name}");

				CurrentPartyCallbacks.TriggerLobbyJoined(CurrentPartyLobby);
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error creating lobby!", e);
				_notificationService.QueueNotification($"Could not create party ({(int) e.Reason})");
			}
		}

		/// <summary>
		/// Joins a party with the given code.
		/// </summary>
		public async UniTask JoinParty(string code)
		{
			Assert.IsNull(CurrentPartyLobby, "Trying to join a party but the player is already in one!");

			var options = new JoinLobbyByCodeOptions
			{
				Player = CreateLocalPlayer()
			};

			try
			{
				FLog.Info($"Joining party with code: {code}");
				CurrentPartyLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
				_partyLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(CurrentPartyLobby.Id, CurrentPartyCallbacks);
				CurrentPartyCallbacks.TriggerLobbyJoined(CurrentPartyLobby);
				FLog.Info(
					$"Party joined! Code: {CurrentPartyLobby.LobbyCode} ID: {CurrentPartyLobby.Id} Name: {CurrentPartyLobby.Name} UPID: {CurrentPartyLobby.Upid}");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error joining party!", e);
				_notificationService.QueueNotification($"Could not join party ({(int) e.Reason})");
			}
		}

		/// <summary>
		/// Invites a friend to the current party.
		/// </summary>
		public async UniTask InviteToParty(string playerID)
		{
			Assert.IsNotNull(CurrentPartyLobby, "Trying to invite a friend to a party but the player is not in one!");

			try
			{
				FLog.Info($"Sending party invite to {playerID}");
				_sentPartyInvites.Add(playerID);
				await FriendsService.Instance.MessageAsync(playerID, FriendMessage.CreatePartyInvite(CurrentPartyLobby.LobbyCode));
				FLog.Info("Party invite sent successfully!");
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error sending party invite!", e);
				_notificationService.QueueNotification($"Could not send party invite ({(int) e.ErrorCode})");
				_sentPartyInvites.Remove(playerID);
			}
		}

		/// <summary>
		/// Leaves the current party.
		/// </summary>
		public async UniTask LeaveParty()
		{
			Assert.IsNotNull(CurrentPartyLobby, "Trying to leave a party but the player is not in one!");

			try
			{
				if (IsPlayerHost(CurrentPartyLobby))
				{
					// Delete the lobby if the player is the host
					FLog.Info($"Deleting party: {CurrentPartyLobby.Id}");
					await LobbyService.Instance.DeleteLobbyAsync(CurrentPartyLobby.Id);
					FLog.Info("Party deleted successfully!");
				}
				else
				{
					// Leave the lobby if the player is not the host
					FLog.Info($"Leaving party: {CurrentPartyLobby.Id}");
					await LobbyService.Instance.RemovePlayerAsync(CurrentPartyLobby.Id, AuthenticationService.Instance.PlayerId);
					FLog.Info("Left party successfully!");
				}

				await _partyLobbyEvents.UnsubscribeAsync();
				_partyLobbyEvents = null;
				CurrentPartyLobby = null;
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error leaving party!", e);
				_notificationService.QueueNotification($"Could not leave party ({(int) e.Reason})");
			}
		}

		/// <summary>
		/// Sets the party host to the given player ID.
		/// </summary>
		public async UniTask<bool> KickPlayerFromParty(string playerID)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to kick player from party but the player is not in one!");

			try
			{
				FLog.Info($"Kicking player: {playerID}");
				await LobbyService.Instance.RemovePlayerAsync(CurrentPartyLobby.Id, playerID);
				FLog.Info("Player kicked successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error kicking player!", e);
				_notificationService.QueueNotification($"Could not kick player ({(int) e.Reason})");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Toggles the ready status of the player in the current party.
		/// </summary>
		public async UniTask TogglePartyReady()
		{
			Assert.IsNotNull(CurrentPartyLobby, "Trying to toggle party ready status but the player is not in one!");

			var currentStatus = CurrentPartyLobby.Players.First(p => p.IsLocal()).IsReady();

			var options = new UpdatePlayerOptions
			{
				Data = new Dictionary<string, PlayerDataObject>
				{
					{
						KEY_READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, (!currentStatus).ToString().ToLowerInvariant())
					}
				}
			};

			try
			{
				FLog.Info($"Setting lobby ready status to: {!currentStatus}");
				CurrentPartyLobby =
					await LobbyService.Instance.UpdatePlayerAsync(CurrentPartyLobby.Id, AuthenticationService.Instance.PlayerId, options);

				FLog.Info("Lobby status set successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error setting ready status!", e);
				_notificationService.QueueNotification($"Could not set ready status ({e.ErrorCode})");
			}
		}

		/// <summary>
		/// Updates the matchmaking ticket for the current party.
		/// </summary>
		public async UniTask<bool> UpdatePartyMatchmakingTicket(JoinedMatchmaking ticket)
		{
			Assert.IsNotNull(CurrentPartyLobby, "Trying to update party matchmaking ticket but the player is not in one!");

			var options = new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{
						KEY_MATCHMAKING_TICKET,
						new DataObject(DataObject.VisibilityOptions.Member, ticket == null ? null : JsonConvert.SerializeObject(ticket))
					}
				}
			};

			try
			{
				FLog.Info("Updating party matchmaking ticket.");
				CurrentPartyLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentPartyLobby.Id, options);
				FLog.Info("Party matchmaking ticket updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error updating party matchmaking ticket!", e);
				_notificationService.QueueNotification($"Could not start party matchmaking ({e.ErrorCode})");

				return false;
			}

			return true;
		}

		/// <summary>
		/// Updates the matchmaking ticket for the current party.
		/// </summary>
		public async UniTask<bool> UpdatePartyMatchmakingGameMode(string modeID)
		{
			Assert.IsNotNull(CurrentPartyLobby, "Trying to update party matchmaking queue but the player is not in one!");

			var options = new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{
						KEY_MATCHMAKING_GAMEMODE, modeID == null ? null : new DataObject(DataObject.VisibilityOptions.Member, modeID)
					}
				}
			};

			try
			{
				FLog.Info("Updating party matchmaking queue.");
				CurrentPartyLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentPartyLobby.Id, options);
				FLog.Info("Party matchmaking queue updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error updating party matchmaking queue!", e);
				_notificationService.QueueNotification($"Could not update party game mode ({e.ErrorCode})");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Queries for public lobbies.
		/// </summary>
		public async UniTask<List<Lobby>> GetPublicMatches(bool allRegions = false)
		{
			var options = new QueryLobbiesOptions
			{
				Filters = new List<QueryFilter>()
			};

			if (!allRegions)
			{
				options.Filters.Add(new QueryFilter(QueryFilter.FieldOptions.S1, _localPrefsService.ServerRegion.Value, QueryFilter.OpOptions.EQ));
			}

			try
			{
				FLog.Info("Fetching match lobbies.");
				var queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
				FLog.Info($"Match lobbies found: {queryResponse.Results.Count}");
				return queryResponse.Results;
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error fetching match lobbies!", e);
				_notificationService.QueueNotification($"Could not fetch games ({(int) e.Reason})");
			}

			return null;
		}
		
		/// <summary>
		/// Creates a new public game lobby.
		/// </summary>
		public async UniTask<bool> CreateMatch(CustomMatchSettings matchOptions)
		{
			Assert.IsNull(CurrentMatchLobby, "Trying to create a match but the player is already in one!");

			var lobbyName = matchOptions.ShowCreatorName
				? string.Format(MATCH_LOBBY_NAME, AuthenticationService.Instance.PlayerName.TrimPlayerNameNumbers())
				: matchOptions.MapID;

			var data = new Dictionary<string, DataObject>
			{
				{KEY_MATCH_SETTINGS, new DataObject(DataObject.VisibilityOptions.Public, JsonConvert.SerializeObject(matchOptions))},
				{
					KEY_REGION,
					new DataObject(DataObject.VisibilityOptions.Public, _localPrefsService.ServerRegion.Value, DataObject.IndexOptions.S1)
				}
			};
			var options = new CreateLobbyOptions
			{
				IsPrivate = matchOptions.PrivateRoom,
				Player = CreateLocalPlayer(),
				Data = data
			};

			try
			{
				FLog.Info($"Creating new match lobby with name: {lobbyName}");
				var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, matchOptions.MaxPlayers, options);
				_matchLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentMatchCallbacks);
				CurrentMatchLobby = lobby;
				CurrentMatchCallbacks.TriggerLobbyJoined(lobby);
				FLog.Info($"Match lobby created! Code: {lobby.LobbyCode} ID: {lobby.Id} Name: {lobby.Name}");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error creating lobby!", e);
				_notificationService.QueueNotification($"Error creating match ({(int) e.Reason})");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Joins a match lobby by id or code.
		/// </summary>
		public async UniTask<bool> JoinMatch(string lobbyIDOrCode)
		{
			Assert.IsNull(CurrentMatchLobby, "Trying to join a match but the player is already in one!");

			try
			{
				FLog.Info($"Joining match with ID/Code: {lobbyIDOrCode}");
				var lobby = lobbyIDOrCode.Length == 6
					? await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyIDOrCode, new JoinLobbyByCodeOptions {Player = CreateLocalPlayer()})
					: await LobbyService.Instance.JoinLobbyByIdAsync(lobbyIDOrCode, new JoinLobbyByIdOptions {Player = CreateLocalPlayer()});
				_matchLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentMatchCallbacks);
				CurrentMatchLobby = lobby;
				CurrentMatchCallbacks.TriggerLobbyJoined(lobby);
				FLog.Info($"Match lobby joined! Code: {lobby.LobbyCode} ID: {lobby.Id} Name: {lobby.Name}");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error joining match!", e);
				_notificationService.QueueNotification($"Could not join match ({(int) e.Reason})");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Invites a friend to the current party.
		/// </summary>
		public async UniTask InviteToMatch(string playerID)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to invite a friend to a party but the player is not in one!");

			try
			{
				FLog.Info($"Sending match invite to {playerID}");
				await FriendsService.Instance.MessageAsync(playerID, FriendMessage.CreateMatchInvite(CurrentMatchLobby.LobbyCode));
				_sentMatchInvites.Add(playerID);
				FLog.Info("Match invite sent successfully!");
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error sending match invite!", e);
				_notificationService.QueueNotification($"Could not send match invite ({(int) e.ErrorCode})");
			}
		}

		/// <summary>
		/// Leaves the current match labby.
		/// </summary>
		public async UniTask LeaveMatch()
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to leave a match but the player is not in one!");

			try
			{
				if (IsPlayerHost(CurrentMatchLobby))
				{
					// Delete the lobby if the player is the host
					FLog.Info($"Deleting match lobby: {CurrentMatchLobby.Id}");
					await LobbyService.Instance.DeleteLobbyAsync(CurrentMatchLobby.Id);
					FLog.Info("Match deleted successfully!");
				}
				else
				{
					// Leave the lobby if the player is not the host
					FLog.Info($"Leaving match lobby: {CurrentMatchLobby.Id}");
					await LobbyService.Instance.RemovePlayerAsync(CurrentMatchLobby.Id, AuthenticationService.Instance.PlayerId);
					FLog.Info("Left match lobby successfully!");
				}

				await _matchLobbyEvents.UnsubscribeAsync();
				_matchLobbyEvents = null;
				CurrentMatchLobby = null;
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error leaving match lobby!", e);
				_notificationService.QueueNotification($"Could not leave match lobby ({(int) e.Reason})");
			}
		}

		/// <summary>
		/// Updates the data / locked state of the current match lobby.
		/// </summary>
		public async UniTask<bool> UpdateMatchLobby(CustomMatchSettings settings, bool locked = false)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to update match settings but the player is not in a match!");

			var options = new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{KEY_MATCH_SETTINGS, new DataObject(DataObject.VisibilityOptions.Public, JsonConvert.SerializeObject(settings))}
				},
				IsLocked = locked,
				MaxPlayers = settings.MaxPlayers
			};

			try
			{
				FLog.Info($"Updating match settings for lobby: {CurrentMatchLobby.Id}");
				CurrentMatchLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentMatchLobby.Id, options);
				FLog.Info("Match settings updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error updating match settings!", e);
				_notificationService.QueueNotification($"Could not update match settings ({(int) e.Reason})");
				return false;
			}

			return true;
		}
		
		/// <summary>
		/// Updates the data / locked state of the current match lobby.
		/// </summary>
		public async UniTask<bool> SetMatchRoom(string roomName)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to update match settings but the player is not in a match!");

			var options = new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{KEY_MATCH_ROOM_NAME, new DataObject(DataObject.VisibilityOptions.Member, roomName)}
				}
			};

			try
			{
				FLog.Info($"Updating match room to {roomName} for lobby: {CurrentMatchLobby.Id}");
				CurrentMatchLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentMatchLobby.Id, options);
				FLog.Info("Match room updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error updating match room!", e);
				_notificationService.QueueNotification($"Could not update match room ({(int) e.Reason})");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Sets the match host to the given player ID.
		/// </summary>
		public async UniTask<bool> UpdateMatchHost(string playerID)
		{
			return (CurrentMatchLobby = await UpdateHost(playerID, CurrentMatchLobby, "match")) != null;
		}

		/// <summary>
		/// Sets the party host to the given player ID.
		/// </summary>
		public async UniTask<bool> UpdatePartyHost(string playerID)
		{
			return (CurrentPartyLobby = await UpdateHost(playerID, CurrentPartyLobby, "party")) != null;
		}

		/// <summary>
		/// Sets the party host to the given player ID.
		/// </summary>
		public async UniTask<bool> KickPlayerFromMatch(string playerID)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to kick player from match but the player is not in one!");

			try
			{
				FLog.Info($"Kicking player: {playerID}");
				await LobbyService.Instance.RemovePlayerAsync(CurrentMatchLobby.Id, playerID);
				FLog.Info("Player kicked successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error kicking player!", e);
				_notificationService.QueueNotification($"Could not kick player ({(int) e.Reason})");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Toggles the spectator status for the current player in the match lobby.
		/// </summary>
		public async UniTask SetMatchSpectator(bool spectating)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to toggle spectator status but the player is not in a match!");
			
			var options = new UpdatePlayerOptions
			{
				Data = new Dictionary<string, PlayerDataObject>
				{
					{
						KEY_SPECTATOR, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, spectating.ToString().ToLowerInvariant())
					}
				}
			};

			try
			{
				FLog.Info($"Setting spectator status to: {spectating}");
				CurrentMatchLobby =
					await LobbyService.Instance.UpdatePlayerAsync(CurrentMatchLobby.Id, AuthenticationService.Instance.PlayerId, options);
				FLog.Info("Spectate status set successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error setting spectate status!", e);
				_notificationService.QueueNotification($"Could not set spectate status ({e.ErrorCode})");
			}
		}

		private async UniTask<Lobby> UpdateHost(string playerID, Lobby lobby, string type)
		{
			Assert.IsNotNull(lobby, $"Trying to update the {type} host but the player is not in one!");

			var options = new UpdateLobbyOptions
			{
				HostId = playerID
			};

			try
			{
				FLog.Info($"Updating {type} host to: {playerID}");
				lobby = await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, options);
				FLog.Info($"{type} host updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn($"Error updating {type} host!", e);
				_notificationService.QueueNotification($"Could not update {type} host ({(int) e.Reason})");
				return null;
			}

			return lobby;
		}

		private async UniTaskVoid Tick()
		{
			FLog.Verbose("Ticking LobbyService");

			while (true)
			{
				await UniTask.WaitForSeconds(TICK_DELAY);

				// Lobbies have to be sent a heartbeat request by the host at least every 30 seconds
				if (CurrentPartyLobby != null && IsPlayerHost(CurrentPartyLobby))
				{
					FLog.Verbose($"Sending party lobby heartbeat to {CurrentPartyLobby.Id}");
					LobbyService.Instance.SendHeartbeatPingAsync(CurrentPartyLobby.Id).AsUniTask().Forget();
				}

				if (CurrentMatchLobby != null && IsPlayerHost(CurrentMatchLobby))
				{
					FLog.Verbose($"Sending game lobby heartbeat to {CurrentMatchLobby.Id}");
					LobbyService.Instance.SendHeartbeatPingAsync(CurrentMatchLobby.Id).AsUniTask().Forget();
				}
			}
			// ReSharper disable once FunctionNeverReturns
		}

		private Player CreateLocalPlayer()
		{
			var skinID = _dataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PLAYER_SKINS).Id;
			var meleeID = _dataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.MELEE_SKINS).Id;

			return new Player(
				id: AuthenticationService.Instance.PlayerId,
				data: new Dictionary<string, PlayerDataObject>
				{
					// TODO mihak: Need to figure out how to get the name from profile but it's always null
					{KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, AuthenticationService.Instance.PlayerName)},
					{KEY_SKIN_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, skinID.ToString())},
					{KEY_MELEE_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, meleeID.ToString())},
					{KEY_READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false")},
					{KEY_PLAYFAB_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayFabSettings.staticPlayer.EntityId)},
					{KEY_SPECTATOR, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false")}
				},
				profile: new PlayerProfile(AuthenticationService.Instance.PlayerName)
			);
		}

		private static bool IsPlayerHost(Lobby lobby)
		{
			return lobby.HostId == AuthenticationService.Instance.PlayerId;
		}

		private void OnMatchLobbyChanged(ILobbyChanges changes)
		{
			FLog.Verbose("Match lobby updated "+changes.Version.Value);
			if (changes.LobbyDeleted)
			{
				CurrentMatchLobby = null;
			}
			else
			{
				changes.ApplyToLobby(CurrentMatchLobby);
			}
			MainInstaller.ResolveServices().MessageBrokerService.Publish(new MatchLobbyUpdatedMessage()
			{
				Changes = changes
			});
		}

		private void OnPartyLobbyChanged(ILobbyChanges changes)
		{
			FLog.Verbose("Party lobby updated version "+changes.Version.Value);
			if (changes.LobbyDeleted)
			{
				CurrentPartyLobby = null;
				MainInstaller.ResolveServices().MessageBrokerService.Publish(new PartyLobbyUpdatedMessage()
				{
					Changes = changes
				});
				return;
			}

			// If a player joined check if we sent the invite and remove it
			if (changes.PlayerJoined.Value != null)
			{
				foreach (var playerJoined in changes.PlayerJoined.Value)
				{
					_sentPartyInvites.Remove(playerJoined.Player.Id);
				}
			}
			changes.ApplyToLobby(CurrentPartyLobby);
			MainInstaller.ResolveServices().MessageBrokerService.Publish(new PartyLobbyUpdatedMessage()
			{
				Changes = changes
			});
		}

		private void OnApplicationQuit(ApplicationQuitMessage _)
		{
			// Delete created lobbies when the player quits the game
			FLog.Verbose("Deleting lobbies on application quit.");

			if (CurrentPartyLobby != null)
			{
				if (CurrentPartyLobby.IsLocalPlayerHost())
				{
					LobbyService.Instance.DeleteLobbyAsync(CurrentPartyLobby.Id);
				}
				else
				{
					LobbyService.Instance.RemovePlayerAsync(CurrentPartyLobby.Id, AuthenticationService.Instance.PlayerId);
				}
			}

			if (CurrentMatchLobby != null)
			{
				if (CurrentMatchLobby.IsLocalPlayerHost())
				{
					LobbyService.Instance.DeleteLobbyAsync(CurrentMatchLobby.Id);
				}
				else
				{
					LobbyService.Instance.RemovePlayerAsync(CurrentMatchLobby.Id, AuthenticationService.Instance.PlayerId);
				}
			}
		}
	}

	public class FLLobbyEventCallbacks : LobbyEventCallbacks
	{
		/// <summary>
		/// Event called when a new lobby is created.
		/// </summary>
		public event Action<Lobby> LobbyJoined;

		public void TriggerLobbyJoined(Lobby lobby)
		{
			LobbyJoined?.Invoke(lobby);
		}
	}
}