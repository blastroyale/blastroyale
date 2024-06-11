using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.SDK.Services;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Notifications;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.Assertions;

namespace FirstLight.Game.Services
{
	public class FLLobbyService
	{
		private const string PARTY_LOBBY_ID = "party_{0}";
		private const int MAX_PARTY_SIZE = 4;
		private const float TICK_DELAY = 15f;

		public const string KEY_SKIN_ID = "skin_id";
		public const string KEY_MELEE_ID = "melee_id";
		public const string KEY_PLAYER_NAME = "player_name";

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
		private readonly List<string> _sentInvites = new ();

		public FLLobbyService(IMessageBrokerService messageBrokerService, IGameDataProvider dataProvider)
		{
			_dataProvider = dataProvider;

			Tick().Forget();

			messageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);

			CurrentPartyCallbacks.LobbyChanged += OnPartyLobbyChanged;
			FriendsService.Instance.MessageReceived += OnFriendMessageReceived;
		}

		private void OnFriendMessageReceived(IMessageReceivedEvent e)
		{
			var message = e.GetAs<PlayerMessage>();

			if (!string.IsNullOrEmpty(message.SquadCode) && CurrentPartyLobby == null)
			{
				// TODO mihak: Ask the player if they want to join
				FLog.Info($"Squad join received: {message.SquadCode}");
				JoinParty(message.SquadCode).Forget();
			}
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
			if (CurrentPartyLobby.IsPlayerHost()) LobbyService.Instance.DeleteLobbyAsync(CurrentPartyLobby.Id);
			if (CurrentMatchLobby.IsPlayerHost()) LobbyService.Instance.DeleteLobbyAsync(CurrentMatchLobby.Id);
		}

		/// <summary>
		/// Creates a new party for the current player with their ID.
		/// </summary>
		public async UniTask CreateParty()
		{
			Assert.IsNull(CurrentPartyLobby, "Trying to create a party but the player is already in one!");

			var lobbyName = string.Format(PARTY_LOBBY_ID, AuthenticationService.Instance.PlayerId);
			var options = new CreateLobbyOptions
			{
				IsPrivate = true,
				Player = CreateLocalPlayer()
			};

			// TODO: Maybe we need to check if the player is already in a party?

			try
			{
				FLog.Info($"Creating new party with ID: {lobbyName}");
				var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PARTY_SIZE, options);
				_partyLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentPartyCallbacks);
				FLog.Info($"Party created! Code: {lobby.LobbyCode} ID: {lobby.Id} Name: {lobby.Name}");

				CurrentPartyLobby = lobby;
			}
			catch (LobbyServiceException e)
			{
				FLog.Error("Error creating lobby!", e);
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
				FLog.Error("Error joining party!", e);
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
				await FriendsService.Instance.MessageAsync(playerID, PlayerMessage.CreateSquadInvite(CurrentPartyLobby.Id));
				_sentInvites.Add(playerID);
				FLog.Info("Party invite sent successfully!");
			}
			catch (FriendsServiceException e)
			{
				FLog.Error("Error sending party invite!", e);
			}
		}

		/// <summary>
		/// Leaves the current party.
		/// </summary>
		public async UniTask LeaveCurrentParty()
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
				FLog.Error("Error leaving party!", e);
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
				FLog.Error("Error updating party host!", e);
			}
		}

		/// <summary>
		/// Queries for public lobbies.
		/// </summary>
		public async UniTask<List<Lobby>> GetPublicGameLobbies()
		{
			var options = new QueryLobbiesOptions();

			try
			{
				FLog.Info("Fetching game lobbies.");
				var queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
				FLog.Info($"Game lobbies found: {queryResponse.Results}");
				return queryResponse.Results;
			}
			catch (LobbyServiceException e)
			{
				FLog.Error("Error fetching game lobbies!", e);
			}

			return null;
		}

		/// <summary>
		/// Creates a new public game lobby.
		/// </summary>
		public async UniTask CreateMatchLobby(CustomGameOptions matchOptions)
		{
			Assert.IsNull(CurrentPartyLobby, "Trying to create a party but the player is already in one!");

			var lobbyName = string.Format(PARTY_LOBBY_ID, AuthenticationService.Instance.PlayerId);
			var options = new CreateLobbyOptions
			{
				IsPrivate = false,
				Player = CreateLocalPlayer()
			};

			// TODO: Maybe we need to check if the player is already in a party?

			try
			{
				FLog.Info($"Creating new party with ID: {lobbyName}");
				var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PARTY_SIZE, options);
				_partyLobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, CurrentPartyCallbacks);
				FLog.Info($"Party created! Code: {lobby.LobbyCode} ID: {lobby.Id} Name: {lobby.Name}");

				CurrentPartyLobby = lobby;
			}
			catch (LobbyServiceException e)
			{
				FLog.Error("Error creating lobby!", e);
			}
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
				data: new Dictionary<string, PlayerDataObject>()
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
	}
}