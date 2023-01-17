using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExitGames.Client.Photon;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace FirstLight.Game.Services
{
	public enum LastDisconnectionLocation
	{
		None,
		Menu,
		Matchmaking,
		FinalPreload,
		Simulation
	}

	/// <summary>
	/// This service provides the possibility to process any network code or to relay backend logic code to a game server
	/// running online.
	/// It gives the possibility to have the desired behaviour for a game to run online.
	/// </summary>
	public interface IGameNetworkService
	{
		/// <summary>
		/// Connects Photon to the master server, using settings in <see cref="IAppDataProvider"/>
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool ConnectPhotonToMaster();

		/// <summary>
		/// Connects Photon to a specific region master server
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool ConnectPhotonToRegionMaster(string region);

		/// <summary>
		/// Connects Photon to the the name server
		/// NOTE: You must disconnect from master serer before connecting to the name server
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool ConnectPhotonToNameServer();

		/// <summary>
		/// Reconnects photon in the most suitable way, based on parameters, after a user was disconnected
		/// </summary>
		/// <param name="inMatchScene">Set true if last disconnection occured in match scene</param>
		/// <param name="requiresManualReconnection">This will be true if disconnected during matchmaking
		/// because during matchmaking, TTL is 0 - disconnected user is booted out of the room.</param>
		void ReconnectPhoton(bool inMatchScene, out bool requiresManualReconnection);

		/// <summary>
		/// Disconnects Photon from whatever server it's currently connected to
		/// </summary>
		void DisconnectPhoton();

		/// <summary>
		/// Join a specified room by name
		/// </summary>
		/// <param name="roomName">Name of the room to join</param>
		/// <returns>True if the operation was sent successfully</returns>
		/// <remarks>Note, in order to join a room, the "entry params" that are generated, need to match a created exactly
		/// for the client to be able to enter. If there is even one param mismatching, join operation will fail.</remarks>
		bool JoinRoom(string roomName);

		/// <summary>
		/// Creates room with a specified name
		/// </summary>
		/// <param name="roomName">Name that the created room will have</param>
		/// <returns>True if the operation was sent successfully</returns>
		bool CreateRoom(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators, string roomName, bool offlineMode);

		/// <summary>
		/// Joins a specific room with matching params if it exists, or creates a new one if it doesn't
		/// </summary>
		/// <param name="roomName">Name of the room to join</param>
		/// <returns>True if the operation was sent successfully</returns>
		/// <remarks>Note, in order to join a room, the "entry params" that are generated, need to match a created room exactly
		/// for the client to be able to enter. If there is even one param mismatching, join operation will fail.</remarks>
		bool JoinOrCreateRoom(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators, string roomName);

		/// <summary>
		/// Joins a random room of matching parameters if it exists, or creates a new one if it doesn't
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool JoinOrCreateRandomRoom(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators);

		/// <summary>
		/// Leaves the current room that local player is in
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool LeaveRoom(bool becomeInactive, bool sendAuthCookie);

		/// <summary>
		/// Raises event to kick specified player from the room. Only works in rooms, as master client.
		/// </summary>
		/// <param name="playerToKick"></param>
		/// <returns>True if the operation was sent successfully</returns>
		bool KickPlayer(Player playerToKick);

		/// <summary>
		/// Updates the spectator status in custom player properties
		/// </summary>
		/// <param name="isSpectator">Is player the spectator</param>
		void SetSpectatePlayerProperty(bool isSpectator);

		/// <summary>
		/// Sets the current room <see cref="Room.IsOpen"/> property, which sets whether it can be joined or not
		/// </summary>
		void SetCurrentRoomOpen(bool isOpen);

		/// <summary>
		/// Requests the current room that the local player is in
		/// </summary>
		Room CurrentRoom { get; }

		/// <summary>
		/// Requests the local player in <see cref="QuantumClient"/>
		/// </summary>
		Player LocalPlayer { get; }
		
		/// <summary>
		/// Returns whether the local player is in a room or not
		/// </summary>
		bool InRoom { get; }

		/// <summary>
		/// Updates/Adds Photon LocalPlayer custom properties
		/// </summary>
		void SetPlayerCustomProperties(Hashtable propertiesToUpdate);

		/// <summary>
		/// Requests the user unique ID for this device
		/// </summary>
		string UserId { get; }

		/// <summary>
		/// Requests the check if the last connection to a room was for a new room (new match), or a rejoin
		/// </summary>
		bool IsJoiningNewMatch { get; }

		// TODO: Replace Player to our own struct RoomPlayer to main player data after the match is over
		/// <summary>
		/// Requests the list of players that the last match was started with
		/// </summary>
		IObservableListReader<Player> LastMatchPlayers { get; }

		/// <summary>
		/// Requests the check if the last disconnection was in matchmaking, before the match started
		/// </summary>
		LastDisconnectionLocation LastDisconnectLocation { get; }

		/// <summary>
		/// Requests the name of the last room that the player disconnected from
		/// </summary>
		string LastConnectedRoomName { get; }

		/// <summary>
		/// Requests the ping status with the quantum server
		/// </summary>
		IObservableFieldReader<bool> HasLag { get; }

		/// <summary>
		/// Requests the current quantum runner configs, from <see cref="IConfigsProvider"/>
		/// </summary>
		QuantumRunnerConfigs QuantumRunnerConfigs { get; }

		/// <inheritdoc cref="QuantumLoadBalancingClient" />
		/// <remarks>Please do not call functions directly from this.
		/// <para>If needs be, implement them inside the service and call those.</para>
		/// <para>This can't be made private because it's used to add callback targets,
		/// has a lot of utils and useful code. Just don't abuse it.</para></remarks>
		QuantumLoadBalancingClient QuantumClient { get; }

		/// <summary>
		/// Requests the current <see cref="QuantumMapConfig"/> for the map set on the current connected room.
		/// If the player is not connected to any room then it return NULL without a value
		/// </summary>
		QuantumMapConfig? CurrentRoomMapConfig { get; }

		/// <summary>
		/// Requests the current <see cref="QuantumGameModeConfig"/> for the game mode set on the current connected room.
		/// If the player is not connected to any room then it return NULL without a value.
		/// </summary>
		QuantumGameModeConfig? CurrentRoomGameModeConfig { get; }

		/// <summary>
		/// Requests the <see cref="MatchType"/> of the currently connected room.
		/// If the player is not connected to any room then it return NULL without a value.
		/// </summary>
		MatchType? CurrentRoomMatchType { get; }

		/// <summary>
		/// Requests the current list of <see cref="QuantumMutatorConfig"/> for the game mode set on the current connected room.
		/// If the player is not connected to any room then it return an empty list.
		/// </summary>
		List<string> CurrentRoomMutatorIds { get; }
	}

	/// <inheritdoc />
	/// <remarks>
	/// This interface allows to manipulate the <see cref="IGameNetworkService"/> data.
	/// The goal for this interface separation is to allow <see cref="FirstLight.Game.StateMachines.NetworkState"/> to
	/// update the network data.
	/// </remarks>
	public interface IGameBackendNetworkService : IGameNetworkService
	{
		/// <inheritdoc cref="IGameNetworkService.UserId" />
		new IObservableField<string> UserId { get; }

		/// <inheritdoc cref="IGameNetworkService.IsJoiningNewMatch" />
		new IObservableField<bool> IsJoiningNewMatch { get; }

		/// <inheritdoc cref="IGameNetworkService.IsJoiningNewMatch" />
		new IObservableList<Player> LastMatchPlayers { get; }

		/// <inheritdoc cref="IGameNetworkService.LastDisconnectLocation" />
		new IObservableField<LastDisconnectionLocation> LastDisconnectLocation { get; }

		/// <inheritdoc cref="IGameNetworkService.LastConnectedRoomName" />
		new IObservableField<string> LastConnectedRoomName { get; }
	}

	/// <inheritdoc cref="IGameNetworkService"/>
	public class GameNetworkService : IGameBackendNetworkService
	{
		private const int LAG_RTT_THRESHOLD_MS = 280;
		private const int STORE_RTT_AMOUNT = 10;
		private const float QUANTUM_TICK_SECONDS = 0.1f;

		private readonly IConfigsProvider _configsProvider;
		private readonly IGameDataProvider _dataProvider;
		private readonly IGameServices _services;

		private bool _isJoiningNewRoom;
		private Queue<int> LastRttQueue;
		private int CurrentRttTotal;

		public IObservableField<string> UserId { get; }
		public IObservableField<bool> IsJoiningNewMatch { get; }
		public IObservableList<Player> LastMatchPlayers { get; }
		public IObservableField<LastDisconnectionLocation> LastDisconnectLocation { get; }
		public IObservableField<string> LastConnectedRoomName { get; }
		public QuantumLoadBalancingClient QuantumClient { get; }
		private IObservableField<bool> HasLag { get; }

		string IGameNetworkService.UserId => UserId.Value;
		bool IGameNetworkService.IsJoiningNewMatch => IsJoiningNewMatch.Value;
		IObservableListReader<Player> IGameNetworkService.LastMatchPlayers => LastMatchPlayers;
		LastDisconnectionLocation IGameNetworkService.LastDisconnectLocation => LastDisconnectLocation.Value;
		string IGameNetworkService.LastConnectedRoomName => LastConnectedRoomName.Value;
		IObservableFieldReader<bool> IGameNetworkService.HasLag => HasLag;
		public Room CurrentRoom => QuantumClient.CurrentRoom;
		public Player LocalPlayer => QuantumClient.LocalPlayer;
		public bool InRoom => QuantumClient.InRoom;

		public QuantumRunnerConfigs QuantumRunnerConfigs => _configsProvider.GetConfig<QuantumRunnerConfigs>();

		/// <inheritdoc />
		public QuantumMapConfig? CurrentRoomMapConfig
		{
			get
			{
				if (!QuantumClient.InRoom)
				{
					return null;
				}

				return _configsProvider.GetConfig<QuantumMapConfig>(QuantumClient.CurrentRoom.GetMapId());
			}
		}

		/// <inheritdoc />
		public QuantumGameModeConfig? CurrentRoomGameModeConfig
		{
			get
			{
				if (!QuantumClient.InRoom)
				{
					return null;
				}

				return _configsProvider.GetConfig<QuantumGameModeConfig>(QuantumClient.CurrentRoom.GetGameModeId()
																					  .GetHashCode());
			}
		}

		public MatchType? CurrentRoomMatchType
		{
			get
			{
				if (!QuantumClient.InRoom)
				{
					return null;
				}

				return QuantumClient.CurrentRoom.GetMatchType();
			}
		}

		public List<string> CurrentRoomMutatorIds
		{
			get
			{
				if (!QuantumClient.InRoom)
				{
					return new List<string>();
				}

				return QuantumClient.CurrentRoom.GetMutatorIds();
			}
		}

		private int RttAverage => CurrentRttTotal / LastRttQueue.Count;

		public GameNetworkService(IConfigsProvider configsProvider, IGameDataProvider gameDataProvider,
								  IGameServices gameServices)
		{
			_configsProvider = configsProvider;
			_dataProvider = gameDataProvider;
			_services = gameServices;
			QuantumClient = new QuantumLoadBalancingClient();
			IsJoiningNewMatch = new ObservableField<bool>(false);
			LastMatchPlayers = new ObservableList<Player>(new List<Player>());
			LastDisconnectLocation = new ObservableField<LastDisconnectionLocation>(LastDisconnectionLocation.None);
			LastConnectedRoomName = new ObservableField<string>("");
			HasLag = new ObservableField<bool>(false);
			UserId = new ObservableResolverField<string>(() => QuantumClient.UserId, SetUserId);
			LastRttQueue = new Queue<int>();
			
			_services.TickService.SubscribeOnUpdate(TickQuantumClient, QUANTUM_TICK_SECONDS, true, true);
			QuantumClient.AddCallbackTarget(this);
		}
		
		private void TickQuantumClient(float deltaTime)
		{
			QuantumClient.Service();
			CalculateUpdateLag();
		}
		
		private void CalculateUpdateLag()
		{
			var newRtt = QuantumClient.LoadBalancingPeer.LastRoundTripTime;
			LastRttQueue.Enqueue(newRtt);
			CurrentRttTotal += newRtt;

			if (LastRttQueue.Count > STORE_RTT_AMOUNT)
			{
				CurrentRttTotal -= LastRttQueue.Dequeue();
			}

			var roundTripCheck = RttAverage > LAG_RTT_THRESHOLD_MS;
			var dcCheck = NetworkUtils.IsOfflineOrDisconnected();

			HasLag.Value = roundTripCheck || dcCheck;
		}

		public bool ConnectPhotonToMaster()
		{
			if (string.IsNullOrEmpty(_dataProvider.AppDataProvider.ConnectionRegion.Value))
			{
				_dataProvider.AppDataProvider.ConnectionRegion.Value = GameConstants.Network.DEFAULT_REGION;
			}

			var settings = QuantumRunnerConfigs.PhotonServerSettings.AppSettings;
			settings.FixedRegion = _dataProvider.AppDataProvider.ConnectionRegion.Value;

			ResetQuantumProperties();

			return QuantumClient.ConnectUsingSettings(settings, _dataProvider.AppDataProvider.DisplayNameTrimmed);
		}

		public bool ConnectPhotonToRegionMaster(string region)
		{
			return QuantumClient.ConnectToRegionMaster(region);
		}

		public bool ConnectPhotonToNameServer()
		{
			return QuantumClient.ConnectToNameServer();
		}

		public void DisconnectPhoton()
		{
			QuantumClient.Disconnect();
		}

		public bool JoinRoom(string roomName)
		{
			if (InRoom) return false;
			
			var enterParams = NetworkUtils.GetRoomEnterParams(roomName);

			QuantumRunnerConfigs.IsOfflineMode = false;

			ResetQuantumProperties();
			SetSpectatePlayerProperty(false);
			IsJoiningNewMatch.Value = true;

			return QuantumClient.OpJoinRoom(enterParams);
		}
		
		public bool CreateRoom(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators, string roomName, bool offlineMode)
		{
			if (InRoom) return false;
			
			var createParams = NetworkUtils.GetRoomCreateParams(gameModeConfig, mapConfig, NetworkUtils.GetRandomDropzonePosRot(), roomName, MatchType.Custom, mutators, false);

			QuantumRunnerConfigs.IsOfflineMode = offlineMode;
			
			ResetQuantumProperties();
			SetSpectatePlayerProperty(false);
			IsJoiningNewMatch.Value = true;
			LastDisconnectLocation.Value = LastDisconnectionLocation.None;

			return QuantumClient.OpCreateRoom(createParams);
		}

		public bool JoinOrCreateRoom(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators, string roomName)
		{
			if (InRoom) return false;
			
			var createParams = NetworkUtils.GetRoomCreateParams(gameModeConfig, mapConfig, NetworkUtils.GetRandomDropzonePosRot(), roomName, MatchType.Custom, mutators, false);

			QuantumRunnerConfigs.IsOfflineMode = false;

			ResetQuantumProperties();
			SetSpectatePlayerProperty(false);
			IsJoiningNewMatch.Value = true;
			LastDisconnectLocation.Value = LastDisconnectionLocation.None;
			
			
			return QuantumClient.OpJoinOrCreateRoom(createParams);
		}

		public bool JoinOrCreateRandomRoom(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators)
		{
			if (InRoom) return false;
			
			var matchType = _services.GameModeService.SelectedGameMode.Value.Entry.MatchType;
			var gameHasBots = gameModeConfig.AllowBots;
			var createParams = NetworkUtils.GetRoomCreateParams(gameModeConfig, mapConfig, NetworkUtils.GetRandomDropzonePosRot(), null, matchType, mutators, gameHasBots);
			var joinRandomParams = NetworkUtils.GetJoinRandomRoomParams(gameModeConfig, mapConfig, matchType, mutators);

			QuantumRunnerConfigs.IsOfflineMode = false;

			ResetQuantumProperties();
			
			SetSpectatePlayerProperty(false);
			IsJoiningNewMatch.Value = true;
			LastDisconnectLocation.Value = LastDisconnectionLocation.None;

			return QuantumClient.OpJoinRandomOrCreateRoom(joinRandomParams, createParams);
		}

		public bool LeaveRoom(bool becomeInactive, bool sendAuthCookie)
		{
			if (!InRoom) return false;
			
			return QuantumClient.OpLeaveRoom(false, true);
		}

		public bool KickPlayer(Player playerToKick)
		{
			if (CurrentRoom == null || !LocalPlayer.IsMasterClient)
			{
				return false;
			}
			
			var eventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.All };
			return QuantumClient.OpRaiseEvent((byte) QuantumCustomEvents.KickPlayer, playerToKick.ActorNumber, eventOptions,
													   SendOptions.SendReliable);
		}

		public void ReconnectPhoton(bool inMatchScene, out bool requiresManualReconnection)
		{
			requiresManualReconnection = false;

			if (QuantumClient.LoadBalancingPeer.PeerState == PeerStateValue.Connecting) return;

			if (inMatchScene)
			{
				IsJoiningNewMatch.Value = false;

				if (LastDisconnectLocation.Value == LastDisconnectionLocation.Matchmaking)
				{
					IsJoiningNewMatch.Value = true;
					SetSpectatePlayerProperty(false);

					// TTL during matchmaking is 0 - we must connect to room manually again by name
					// Rejoining room is handled OnMasterConnected
					requiresManualReconnection = true;
					QuantumClient.ReconnectToMaster();
				}
				else
				{
					QuantumClient.ReconnectAndRejoin();
				}
			}
			else
			{
				QuantumClient.ReconnectToMaster();
			}
		}

		public void SetCurrentRoomOpen(bool isOpen)
		{
			CurrentRoom.IsOpen = isOpen;
		}

		public void SetPlayerCustomProperties(Hashtable propertiesToUpdate)
		{
			QuantumClient.LocalPlayer.SetCustomProperties(propertiesToUpdate);
		}

		public void SetSpectatePlayerProperty(bool isSpectator)
		{
			var playerPropsUpdate = new Hashtable
			{
				{
					GameConstants.Network.PLAYER_PROPS_SPECTATOR, isSpectator
				}
			};

			SetPlayerCustomProperties(playerPropsUpdate);
		}

		private void SetUserId(string id)
		{
			QuantumClient.UserId = id;
			QuantumClient.AuthValues.AuthGetParameters = "";

			QuantumClient.AuthValues.AddAuthParameter("username", id);
		}

		private void ResetQuantumProperties()
		{
			QuantumClient.AuthValues.AuthType = CustomAuthenticationType.Custom;
			QuantumClient.EnableProtocolFallback = true;
			QuantumClient.NickName = _dataProvider.AppDataProvider.DisplayNameTrimmed;

			var preloadIds = new List<int>();

			foreach (var item in _dataProvider.EquipmentDataProvider.Loadout)
			{
				var equipmentDataInfo = _dataProvider.EquipmentDataProvider.Inventory[item.Value];
				preloadIds.Add((int) equipmentDataInfo.GameId);
			}

			preloadIds.Add((int) _dataProvider.PlayerDataProvider.PlayerInfo.Skin);

			var playerProps = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_PRELOAD_IDS, preloadIds.ToArray()},
				{GameConstants.Network.PLAYER_PROPS_CORE_LOADED, false},
				{GameConstants.Network.PLAYER_PROPS_ALL_LOADED, false},
				{GameConstants.Network.PLAYER_PROPS_SPECTATOR, false}
			};

			QuantumClient.LocalPlayer.SetCustomProperties(playerProps);
		}
	}
}