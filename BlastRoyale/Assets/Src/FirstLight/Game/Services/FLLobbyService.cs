using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.SDK.Services;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.Assertions;

namespace FirstLight.Game.Services
{
	public class FLLobbyService
	{
		private const string PARTY_LOBBY_NAME = "party_{0}";
		private const string MATCH_LOBBY_NAME = "{0}'s game";
		private const int MAX_PARTY_SIZE = 4;
		private const float TICK_DELAY = 15f;

		public const string KEY_SKIN_ID = "skin_id";
		public const string KEY_MELEE_ID = "melee_id";
		public const string KEY_PLAYER_NAME = "player_name";

		public const string KEY_MATCH_SETTINGS = "match_settings";

		/// <summary>
		/// The party the player is currently in.
		/// </summary>
		public Lobby CurrentPartyLobby { get; private set; }

		/// <summary>
		/// Events that trigger when the party lobby changes.
		/// </summary>
		public LobbyEventCallbacks CurrentPartyCallbacks { get; } = new ();

		/// <summary>
		/// The IDs of the players we sent invites to (only persists for the current session).
		/// </summary>
		public IReadOnlyList<string> SentInvites => _sentInvites;

		/// <summary>
		/// The custom game the player is currently in.
		/// </summary>
		public Lobby CurrentMatchLobby { get; private set; }

		/// <summary>
		/// Events that trigger when the custom game lobby changes.
		/// </summary>
		public LobbyEventCallbacks CurrentMatchCallbacks { get; private set; } = new ();

		private ILobbyEvents _partyLobbyEvents;
		private ILobbyEvents _matchLobbyEvents;

		private readonly IGameDataProvider _dataProvider;
		private readonly NotificationService _notificationService;
		private readonly List<string> _sentInvites = new ();

		public FLLobbyService(IMessageBrokerService messageBrokerService, IGameDataProvider dataProvider, NotificationService notificationService)
		{
			_dataProvider = dataProvider;
			_notificationService = notificationService;

			Tick().Forget();

			messageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);

			CurrentPartyCallbacks.LobbyChanged += OnPartyLobbyChanged;
			CurrentMatchCallbacks.LobbyChanged += OnMatchLobbyChanged;
		}

		/// <summary>
		/// Creates a new party for the current player with their ID.
		/// </summary>
		public async UniTask CreateParty()
		{
			Assert.IsNull(CurrentPartyLobby, "Trying to create a party but the player is already in one!");

			var lobbyName = string.Format(PARTY_LOBBY_NAME, AuthenticationService.Instance.PlayerId);
			var options = new CreateLobbyOptions
			{
				IsPrivate = true,
				Player = CreateLocalPlayer()
			};

			// TODO: Maybe we need to check if the player is already in a party?

			try
			{
				FLog.Info($"Creating new party with name: {lobbyName}");
				var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PARTY_SIZE, options);
				_partyLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentPartyCallbacks);
				FLog.Info($"Party created! Code: {lobby.LobbyCode} ID: {lobby.Id} Name: {lobby.Name}");

				CurrentPartyLobby = lobby;
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

			var options = new JoinLobbyByCodeOptions()
			{
				Player = CreateLocalPlayer()
			};

			try
			{
				FLog.Info($"Joining party with code: {code}");
				var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
				_partyLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentPartyCallbacks);
				CurrentPartyLobby = lobby;
				FLog.Info($"Party joined! Code: {lobby.LobbyCode} ID: {lobby.Id} Name: {lobby.Name} UPID: {lobby.Upid}");
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
				await FriendsService.Instance.MessageAsync(playerID, FriendMessage.CreatePartyInvite(CurrentPartyLobby.LobbyCode));
				_sentInvites.Add(playerID);
				FLog.Info("Party invite sent successfully!");
			}
			catch (FriendsServiceException e)
			{
				FLog.Warn("Error sending party invite!", e);
				_notificationService.QueueNotification($"Could not send party invite ({(int) e.ErrorCode})");
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
		public async UniTask UpdatePartyHost(string playerID)
		{
			Assert.IsNotNull(CurrentPartyLobby, "Trying to update the party host but the player is not in one!");

			var options = new UpdateLobbyOptions
			{
				HostId = playerID
			};

			try
			{
				FLog.Info($"Updating party host to: {playerID}");
				CurrentPartyLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentPartyLobby.Id, options);
				FLog.Info("Party host updated successfully!");
			}
			catch (LobbyServiceException e)
			{
				FLog.Warn("Error updating party host!", e);
				_notificationService.QueueNotification($"Could not update party host ({(int) e.Reason})");
			}
		}

		/// <summary>
		/// Queries for public lobbies.
		/// </summary>
		public async UniTask<List<Lobby>> GetPublicMatches()
		{
			var options = new QueryLobbiesOptions();

			try
			{
				FLog.Info("Fetching match lobbies.");
				var queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
				FLog.Info($"Match lobbies found: {queryResponse.Results}");
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

			// TODO: What should we use when not showing creator name?
			var lobbyName = matchOptions.ShowCreatorName
				? string.Format(MATCH_LOBBY_NAME, AuthenticationService.Instance.PlayerName)
				: matchOptions.MapID;
			var options = new CreateLobbyOptions
			{
				IsPrivate = false,
				Player = CreateLocalPlayer(),
				Data = new Dictionary<string, DataObject>
				{
					{KEY_MATCH_SETTINGS, new DataObject(DataObject.VisibilityOptions.Public, JsonConvert.SerializeObject(matchOptions))}
				}
			};

			try
			{
				FLog.Info($"Creating new match lobby with name: {lobbyName}");
				var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, matchOptions.MaxPlayers, options);
				_matchLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentMatchCallbacks);
				CurrentMatchLobby = lobby;
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

		public async UniTask<bool> JoinMatch(string lobbyIDOrCode)
		{
			Assert.IsNull(CurrentMatchLobby, "Trying to join a match but the player is already in one!");

			var options = new JoinLobbyByIdOptions
			{
				Player = CreateLocalPlayer()
			};

			try
			{
				FLog.Info($"Joining match with ID/Code: {lobbyIDOrCode}");
				var lobby = lobbyIDOrCode.Length == 6
					? await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyIDOrCode)
					: await LobbyService.Instance.JoinLobbyByIdAsync(lobbyIDOrCode, options);
				_matchLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentMatchCallbacks);
				CurrentMatchLobby = lobby;
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

		public async UniTask<bool> UpdateMatchSettings(CustomMatchSettings settings)
		{
			Assert.IsNotNull(CurrentMatchLobby, "Trying to update match settings but the player is not in a match!");

			var options = new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{KEY_MATCH_SETTINGS, new DataObject(DataObject.VisibilityOptions.Public, JsonConvert.SerializeObject(settings))}
				}
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
					// TODO mihak: Need to figure out how to get this from profile but it's always null
					{KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, AuthenticationService.Instance.PlayerName)},
					{KEY_SKIN_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, skinID.ToString())},
					{KEY_MELEE_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, meleeID.ToString())}
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
			if (changes.LobbyDeleted)
			{
				CurrentMatchLobby = null;
				return;
			}

			changes.ApplyToLobby(CurrentMatchLobby);
		}

		private void OnPartyLobbyChanged(ILobbyChanges changes)
		{
			if (changes.LobbyDeleted)
			{
				CurrentPartyLobby = null;
				return;
			}

			// If a player joined check if we sent the invite and remove it
			foreach (var playerJoined in changes.PlayerJoined.Value)
			{
				_sentInvites.Remove(playerJoined.Player.Id);
			}

			changes.ApplyToLobby(CurrentPartyLobby);
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
					// TODO: We should not remove player, we should try to reconnect to the lobby
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
					// TODO: We should not remove player, we should try to reconnect to the lobby
					LobbyService.Instance.RemovePlayerAsync(CurrentMatchLobby.Id, AuthenticationService.Instance.PlayerId);
				}
			}
		}
	}
}