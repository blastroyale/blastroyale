using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services.Social;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK.Popups;
using FirstLight.SDK.Services;
using Newtonsoft.Json;
using PlayFab;
using Quantum;
using Src.FirstLight.Game.Utils;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Assert = UnityEngine.Assertions.Assert;
using Player = Unity.Services.Lobbies.Models.Player;

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
		/// Toggles the ready status of the player in the current match.
		/// </summary>
		UniTask ToggleMatchReady();

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

		/// <summary>
		/// Requests a specific position in the match lobby. The request has to be handled
		/// by the host before it is valid and updated in the positions array.
		/// </summary>
		void SetMatchPositionRequest(int position);

		/// <summary>
		/// Sets match property.
		/// </summary>
		UniTask<bool> SetMatchProperty(string name, string value);

		/// <summary>
		/// Sets match player property.
		/// Can only set for current players
		/// </summary>
		UniTask<bool> SetMatchPlayerProperty(string name, string value, bool silent = true);
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
		public const string KEY_POSITION_REQUEST = "position_request";
		public const string KEY_MATCHMAKING_TICKET = "matchmaking_ticket";
		public const string KEY_MATCHMAKING_GAMEMODE = "matchmaking_gamemode";

		public const string KEY_LOBBY_MATCH_PLAYER_POSITIONS = "player_positions";
		public const string KEY_LOBBY_MATCH_SETTINGS = "match_settings";
		public const string KEY_LOBBY_MATCH_ROOM_NAME = "room_name";
		public const string KEY_LOBBY_MATCH_REGION = "region"; // S1

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
		private readonly LobbyGrid _grid = new ();
		private readonly AsyncBufferedQueue _matchUpdateQueue = new (TimeSpan.FromSeconds(1), true);

		private bool _leaving;

		public FLLobbyService(IMessageBrokerService messageBrokerService, IGameDataProvider dataProvider, NotificationService notificationService,
							  LocalPrefsService localPrefsService)
		{
			_dataProvider = dataProvider;
			_notificationService = notificationService;
			_localPrefsService = localPrefsService;

			Tick().Forget();

			messageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);

			CurrentMatchCallbacks.PlayerJoined += OnMatchPlayerJoined;
			CurrentMatchCallbacks.PlayerLeft += OnMatchPlayerLeft;
			CurrentPartyCallbacks.LobbyChanged += OnPartyLobbyChanged;
			CurrentMatchCallbacks.LobbyChanged += OnMatchLobbyChanged;
			CurrentMatchCallbacks.KickedFromLobby += OnMatchLobbyKicked;
			CurrentPartyCallbacks.KickedFromLobby += OnPartyLobbyKicked;
			CurrentMatchCallbacks.LobbyDeleted += OnMatchDeleted;
			CurrentPartyCallbacks.LobbyDeleted += OnPartyDeleted;
		}

		private void OnMatchPlayerLeft(List<int> playerIds)
		{
			_grid.EnqueueGridSync(CurrentMatchLobby);
		}

		private void OnMatchPlayerJoined(List<LobbyPlayerJoined> players)
		{
			_grid.EnqueueGridSync(CurrentMatchLobby);
		}

		private void OnMatchDeleted()
		{
			_sentMatchInvites.Clear();
			CurrentMatchLobby = null;
		}

		private void OnPartyDeleted()
		{
			_sentMatchInvites.Clear();
		}

		#region TEAMS

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
				_notificationService.QueueNotification($"Could not create party, {e.ParseError()}");
			}
		}

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
				CurrentPartyLobby = await LobbyService.Instance.GetLobbyAsync(CurrentPartyLobby.Id); // ensure we don't miss events
				CurrentPartyCallbacks.TriggerLobbyJoined(CurrentPartyLobby);
				FLog.Info(
					$"Party joined! Code: {CurrentPartyLobby.LobbyCode} ID: {CurrentPartyLobby.Id} Name: {CurrentPartyLobby.Name} UPID: {CurrentPartyLobby.Upid}");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error joining party!", e);
				_notificationService.QueueNotification($"Could not join party {e.ParseError()}");
			}
		}

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
				_notificationService.QueueNotification($"Could not send party invite: {e.ErrorCode.ToStringSeparatedWords()}");
				_sentPartyInvites.Remove(playerID);
			}
		}

		public async UniTask LeaveParty()
		{
			Assert.IsNotNull(CurrentPartyLobby, "Trying to leave a party but the player is not in one!");

			try
			{
				if (IsLocalPlayerHost(CurrentPartyLobby))
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
				_notificationService.QueueNotification($"Could not leave party, {e.ParseError()}");
			}
		}

		public async UniTask<bool> KickPlayerFromParty(string playerID)
		{
			Assert.IsNotNull(CurrentPartyLobby, "Trying to kick player from party but the player is not in one!");

			try
			{
				FLog.Info($"Kicking player: {playerID}");
				await LobbyService.Instance.RemovePlayerAsync(CurrentPartyLobby.Id, playerID);
				FLog.Info("Player kicked successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error kicking player!", e);
				_notificationService.QueueNotification($"Could not kick player, {e.ParseError()}");
				return false;
			}

			return true;
		}

		public async UniTask TogglePartyReady()
		{
			CurrentPartyLobby = await ToggleReady(CurrentPartyLobby);
		}

		public UniTask<bool> UpdatePartyMatchmakingTicket(JoinedMatchmaking ticket)
		{
			return SetPartyProperty(KEY_MATCHMAKING_TICKET, ticket == null ? null : JsonConvert.SerializeObject(ticket));
		}

		public UniTask<bool> UpdatePartyMatchmakingGameMode(string modeID)
		{
			return SetPartyProperty(KEY_MATCHMAKING_TICKET, modeID);
		}

		#endregion

		#region LOBBIES

		public async UniTask<Lobby> SetHost(Lobby lobby, string playerID)
		{
			var options = new UpdateLobbyOptions
			{
				HostId = playerID
			};
			try
			{
				FLog.Info($"Updating host to: {playerID}");
				return await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, options);
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn($"Error updating host!", e);
				_notificationService.QueueNotification($"Could not update host, {e.ParseError()}");
				return null;
			}
		}

		public async UniTask<bool> SetMatchProperty(string name, string value)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Not in match!");
			var options = new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{name, new DataObject(DataObject.VisibilityOptions.Member, value)},
				}
			};
			try
			{
				FLog.Info($"Updating lobby: {CurrentMatchLobby.Id}, {name}:{value}");
				CurrentMatchLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentMatchLobby.Id, options);
				FLog.Info("Lobby updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error updating lobby!", e);
				_notificationService.QueueNotification($"Could not update lobby, {e.ParseError()}");
				return false;
			}

			return true;
		}

		private async UniTask<bool> SetPartyProperty(string name, string value)
		{
			Assert.IsNotNull(CurrentPartyLobby, "Not in a party!");
			var options = new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{name, new DataObject(DataObject.VisibilityOptions.Member, value)},
				}
			};
			try
			{
				FLog.Info($"Updating lobby: {CurrentPartyLobby.Id}, {name}:{value}");
				CurrentPartyLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentPartyLobby.Id, options);
				FLog.Info("Lobby updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error updating lobby!", e);
				_notificationService.QueueNotification($"Could not update lobby, {e.ParseError()}");
				return false;
			}

			return true;
		}

		public async UniTask<bool> SetMatchPlayerProperty(string name, string value, bool silent = true)
		{
			var options = new UpdatePlayerOptions()
			{
				Data = new Dictionary<string, PlayerDataObject>
				{
					{name, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, value)},
				}
			};
			try
			{
				FLog.Info($"Updating lobby: {CurrentMatchLobby.Id} player {AuthenticationService.Instance.PlayerId}");
				CurrentMatchLobby =
					await LobbyService.Instance.UpdatePlayerAsync(CurrentMatchLobby.Id, AuthenticationService.Instance.PlayerId, options);
				FLog.Info("Lobby player updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error updating lobby player!", e);
				if (silent) return false;
				_notificationService.QueueNotification($"Could not update player, {e.ParseError()}");
				return false;
			}

			return true;
		}

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
				_notificationService.QueueNotification($"Could not fetch games, {e.ParseError()}");
			}

			return null;
		}

		public async UniTask<bool> CreateMatch(CustomMatchSettings matchOptions)
		{
			Assert.IsNull(CurrentMatchLobby, "Trying to create a match but the player is already in one!");

			var lobbyName = matchOptions.ShowCreatorName
				? string.Format(MATCH_LOBBY_NAME, AuthenticationService.Instance.PlayerName.TrimPlayerNameNumbers())
				: Enum.Parse<GameId>(matchOptions.MapID).GetLocalization();

			var positions = new string[matchOptions.MaxPlayers];
			positions[0] = AuthenticationService.Instance.PlayerId;

			var data = new Dictionary<string, DataObject>
			{
				{KEY_LOBBY_MATCH_SETTINGS, new DataObject(DataObject.VisibilityOptions.Public, JsonConvert.SerializeObject(matchOptions))},
				{
					KEY_LOBBY_MATCH_REGION,
					new DataObject(DataObject.VisibilityOptions.Public, _localPrefsService.ServerRegion.Value, DataObject.IndexOptions.S1)
				},
				{KEY_LOBBY_MATCH_PLAYER_POSITIONS, new DataObject(DataObject.VisibilityOptions.Member, string.Join(',', positions))}
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
				_notificationService.QueueNotification($"Error creating match, {e.ParseError()}");
				return false;
			}

			return true;
		}

		public async UniTask<bool> JoinMatch(string lobbyIDOrCode)
		{
			Assert.IsNull(CurrentMatchLobby, "Trying to join a match but the player is already in one!");

			try
			{
				var isLobbyCode = lobbyIDOrCode.Length == 6;
				FLog.Info($"Joining match with ID/Code: {lobbyIDOrCode}");
				var lobby = isLobbyCode
					? await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyIDOrCode, new JoinLobbyByCodeOptions {Player = CreateLocalPlayer()})
					: await LobbyService.Instance.JoinLobbyByIdAsync(lobbyIDOrCode, new JoinLobbyByIdOptions {Player = CreateLocalPlayer()});
				_matchLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentMatchCallbacks);
				CurrentMatchLobby = await LobbyService.Instance.GetLobbyAsync(lobby.Id); // to ensure we don't miss events
				CurrentMatchCallbacks.TriggerLobbyJoined(lobby);
				FLog.Info($"Match lobby joined! Code: {lobby.LobbyCode} ID: {lobby.Id} Name: {lobby.Name}");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error joining match!", e);
				_notificationService.QueueNotification($"Could not join match, {e.ParseError()}");
				return false;
			}

			return true;
		}

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
				_notificationService.QueueNotification($"Could not send match invite, {e.ErrorCode.ToStringSeparatedWords()}");
			}
		}

		public async UniTask LeaveMatch()
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to leave a match but the player is not in one!");

			_leaving = true;
			try
			{
				if (IsLocalPlayerHost(CurrentMatchLobby))
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
				_notificationService.QueueNotification($"Could not leave match lobby, {e.ParseError()}");
			}
			finally
			{
				_leaving = false;
			}
		}

		public UniTask<bool> UpdateMatchLobby(CustomMatchSettings settings, bool locked = false)
		{
			_matchUpdateQueue.Add(async () =>
			{
				Assert.IsNotNull(CurrentMatchLobby, "Trying to update match settings but the player is not in a match!");

				var lobbyName = settings.ShowCreatorName
					? string.Format(MATCH_LOBBY_NAME, AuthenticationService.Instance.PlayerName.TrimPlayerNameNumbers())
					: Enum.Parse<GameId>(settings.MapID).GetLocalization();

				var options = new UpdateLobbyOptions
				{
					Data = new Dictionary<string, DataObject>
					{
						{KEY_LOBBY_MATCH_SETTINGS, new DataObject(DataObject.VisibilityOptions.Public, JsonConvert.SerializeObject(settings))}
					},
					IsLocked = locked,
					MaxPlayers = settings.MaxPlayers,
					Name = lobbyName
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
					_notificationService.QueueNotification($"Could not update match settings, {e.ParseError()}");
				}
			});
			return UniTask.FromResult(false);
		}

		public async UniTask<bool> SetMatchRoom(string roomName)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to update match settings but the player is not in a match!");

			var options = new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{KEY_LOBBY_MATCH_ROOM_NAME, new DataObject(DataObject.VisibilityOptions.Member, roomName)},
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
				_notificationService.QueueNotification($"Could not update match room, {e.ParseError()}");
				return false;
			}

			return true;
		}

		public async UniTask ToggleMatchReady()
		{
			CurrentMatchLobby = await ToggleReady(CurrentMatchLobby);
		}

		public async UniTask<bool> UpdateMatchHost(string playerID)
		{
			return (CurrentMatchLobby = await SetHost(CurrentMatchLobby, playerID)) != null;
		}

		public async UniTask<bool> UpdatePartyHost(string playerID)
		{
			return (CurrentPartyLobby = await SetHost(CurrentPartyLobby, playerID)) != null;
		}

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
				_notificationService.QueueNotification($"Could not kick player, {e.ParseError()}");
				return false;
			}

			return true;
		}

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
				_notificationService.QueueNotification($"Could not set spectate status, {e.ParseError()}");
			}
		}

		public void SetMatchPositionRequest(int position)
		{
			_grid.RequestToGoToPosition(CurrentMatchLobby, position);
		}

		public async UniTask<Lobby> ToggleReady(Lobby lobby)
		{
			Assert.IsNotNull(lobby, "Trying to toggle ready status but the player is not in a lobby!");

			var currentStatus = lobby.Players.First(p => p.IsLocal()).IsReady();

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
				return await LobbyService.Instance.UpdatePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId, options);
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error setting ready status!", e);
				_notificationService.QueueNotification($"Could not set ready status, {e.ParseError()}");
			}

			return null;
		}

		private async UniTaskVoid Tick()
		{
			FLog.Verbose("Ticking LobbyService");

			while (true)
			{
				await UniTask.WaitForSeconds(TICK_DELAY);

				if (MainInstaller.ResolveServices().RoomService.InRoom) return;

				// Lobbies have to be sent a heartbeat request by the host at least every 30 seconds
				if (CurrentPartyLobby != null && IsLocalPlayerHost(CurrentPartyLobby))
				{
					FLog.Verbose($"Sending party lobby heartbeat to {CurrentPartyLobby.Id}");
					LobbyService.Instance.SendHeartbeatPingAsync(CurrentPartyLobby.Id).AsUniTask().Forget();
				}

				if (CurrentMatchLobby != null && IsLocalPlayerHost(CurrentMatchLobby))
				{
					FLog.Verbose($"Sending game lobby heartbeat to {CurrentMatchLobby.Id}");
					LobbyService.Instance.SendHeartbeatPingAsync(CurrentMatchLobby.Id).AsUniTask().Forget();
				}
			}
			// ReSharper disable once FunctionNeverReturns
		}

		#endregion

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

		private static bool IsLocalPlayerHost(Lobby lobby)
		{
			return lobby.HostId == AuthenticationService.Instance.PlayerId;
		}

		private void OnMatchLobbyChanged(ILobbyChanges changes)
		{
			if (CurrentMatchLobby == null) return;

			if (changes.LobbyDeleted)
			{
				CurrentMatchLobby = null;
			}
			else
			{
				changes.ApplyToLobby(CurrentMatchLobby);
			}

			CurrentMatchCallbacks.TriggerLocalLobbyUpdated(changes);
			if (CurrentMatchLobby != null)
			{
				if (CurrentMatchLobby.IsLocalPlayerHost())
				{
					_grid.EnqueueGridSync(CurrentMatchLobby);
				}

				_grid.HandleLobbyUpdates(CurrentMatchLobby, changes).Forget();
			}
		}

		private void OnMatchLobbyKicked()
		{
			if (CurrentMatchLobby == null) return;

			var service = MainInstaller.ResolveServices().RoomService;
			if (!service.InRoom && !service.IsJoiningRoom && !_leaving)
			{
				_notificationService.QueueNotification("You have been kicked from the lobby.");
			}

			_sentMatchInvites.Clear();
			CurrentMatchLobby = null;
		}

		private void OnPartyLobbyKicked()
		{
			CurrentPartyLobby = null;
			if (!PopupPresenter.IsOpen<PartyPopupView>())
			{
				_notificationService.QueueNotification($"You left the team");
			}

			_sentPartyInvites.Clear();
			CurrentPartyCallbacks.TriggerLocalLobbyUpdated(null);
		}

		private void OnPartyLobbyChanged(ILobbyChanges changes)
		{
			FLog.Verbose("Party lobby updated version " + changes.Version.Value);
			if (changes.LobbyDeleted)
			{
				CurrentPartyLobby = null;
				CurrentPartyCallbacks.TriggerLocalLobbyUpdated(changes);
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
			CurrentPartyCallbacks.TriggerLocalLobbyUpdated(changes);
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
		public event Action<Lobby> LocalLobbyJoined;

		/// <summary>
		/// Called after a lobby update is received and proccessed so the local lobby is up-to-date too
		/// </summary>
		public event Action<ILobbyChanges> LocalLobbyUpdated;

		public void TriggerLobbyJoined(Lobby lobby)
		{
			LocalLobbyJoined?.Invoke(lobby);
		}

		public void TriggerLocalLobbyUpdated(ILobbyChanges changes)
		{
			LocalLobbyUpdated?.Invoke(changes);
		}
	}
}