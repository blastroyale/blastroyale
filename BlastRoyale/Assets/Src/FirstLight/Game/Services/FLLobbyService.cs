using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.SDK.Services;
using Unity.Services.Authentication;
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

		/// <summary>
		/// The party the player is currently in.
		/// </summary>
		public Lobby CurrentPartyLobby { get; private set; }

		/// <summary>
		/// Events that trigger when the party lobby changes.
		/// </summary>
		public LobbyEventCallbacks CurrentPartyCallbacks { get; } = new ();

		/// <summary>
		/// The custom game the player is currently in.
		/// </summary>
		public Lobby CurrentGameLobby { get; private set; }

		/// <summary>
		/// Events that trigger when the custom game lobby changes.
		/// </summary>
		public LobbyEventCallbacks CurrentGameCallbacks { get; private set; } = new ();

		private ILobbyEvents _partyLobbyEvents;
		private ILobbyEvents _gameLobbyEvents;

		private readonly IGameDataProvider _dataProvider;

		public FLLobbyService(IMessageBrokerService messageBrokerService, IGameDataProvider dataProvider)
		{
			_dataProvider = dataProvider;
			
			Tick().Forget();

			messageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);
		}

		private void OnApplicationQuit(ApplicationQuitMessage _)
		{
			// Delete created lobbies when the player quits the game
			FLog.Verbose("Deleting lobbies on application quit.");
			if (CurrentPartyLobby.IsPlayerHost()) LobbyService.Instance.DeleteLobbyAsync(CurrentPartyLobby.Id);
			if (CurrentGameLobby.IsPlayerHost()) LobbyService.Instance.DeleteLobbyAsync(CurrentGameLobby.Id);
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

				if (CurrentGameLobby != null && IsPlayerHost(CurrentGameLobby))
				{
					FLog.Verbose($"Sending game lobby heartbeat to {CurrentGameLobby.Id}");
					LobbyService.Instance.SendHeartbeatPingAsync(CurrentGameLobby.Id).AsUniTask().Forget();
				}
			}
			// ReSharper disable once FunctionNeverReturns
		}

		private Player CreateLocalPlayer()
		{
			var skinID = _dataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PLAYER_SKINS).Id;
			var meleeID = _dataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.MELEE_SKINS).Id;
			
			return new Player(
				AuthenticationService.Instance.PlayerId,
				null,
				new Dictionary<string, PlayerDataObject>()
				{
					{KEY_SKIN_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, skinID.ToString())},
					{KEY_MELEE_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, meleeID.ToString())}
				}
			);
		}

		private static bool IsPlayerHost(Lobby lobby)
		{
			return lobby.HostId == AuthenticationService.Instance.PlayerId;
		}
	}
}