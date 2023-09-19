using System.Collections;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Photon.Realtime;
using UnityEngine;
using Quantum;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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
		/// This will connect to nameserver if no region is specified in photon settings (photon default behaviour)
		/// After connecting to nameserver and pinging regions it will connect to master straight away
		/// If a region is specified, it will connect directly to master
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool ConnectPhotonServer();

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
		/// <param name="requiresManualReconnection">This will be true if disconnected during matchmaking
		/// because during matchmaking, TTL is 0 - disconnected user is booted out of the room.</param>
		void ReconnectPhoton(out bool requiresManualReconnection);

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
		/// Rejoins a room, only if the player is still active in the room. Will never enter a room creating a new player.
		/// The room creation params must match the room params when created.
		/// </summary>
		bool RejoinRoom(string name);

		/// <summary>
		/// Creates room with a specified name
		/// </summary>
		/// <param name="roomName">Name that the created room will have</param>
		/// <returns>True if the operation was sent successfully</returns>
		bool CreateRoom(MatchRoomSetup setup, bool offlineMode);

		/// <summary>
		/// Joins a specific room with matching params if it exists, or creates a new one if it doesn't
		/// </summary>
		/// <param name="roomName">Name of the room to join</param>
		/// <returns>True if the operation was sent successfully</returns>
		/// <remarks>Note, in order to join a room, the "entry params" that are generated, need to match a created room exactly
		/// for the client to be able to enter. If there is even one param mismatching, join operation will fail.</remarks>
		bool JoinOrCreateRoom(MatchRoomSetup setup, string teamID = null, string[] expectedPlayers = null);

		/// <summary>
		/// Joins a random room of matching parameters if it exists, or creates a new one if it doesn't
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool JoinOrCreateRandomRoom(MatchRoomSetup setup);

		/// <summary>
		/// Leaves the current room that local player is in
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool LeaveRoom(bool becomeInactive, bool sendAuthCookie);

		/// <summary>
		/// Sends user token to Quantum Server to prove the user is authenticated and able to send commands.
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool SendPlayerToken(string token);

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
		/// Sets a team ID manually (for custom games).
		/// </summary>
		void SetManualTeamId(string teamId);

		/// <summary>
		/// Sets the TeamID (for squads) in custom properties (-1 means solo).
		/// </summary>
		public void SetDropPosition(Vector2 dropPosition);

		/// <summary>
		/// Sets the current room <see cref="Room.IsOpen"/> property, which sets whether it can be joined or not
		/// </summary>
		void SetCurrentRoomOpen(bool isOpen);

		/// <summary>
		/// Updates/Adds Photon LocalPlayer custom properties
		/// </summary>
		void SetPlayerCustomProperties(Hashtable propertiesToUpdate);

		/// <summary>
		/// Enables or disables client ticking update and lag detection
		/// </summary>
		void EnableClientUpdate(bool enabled);

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
		/// Requests the user unique ID for this device
		/// </summary>
		string UserId { get; }

		/// <summary>
		/// Requests the check if the last connection to a room was for a new room (new match), or a rejoin
		/// </summary>
		JoinRoomSource JoinSource { get; }

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
		Room LastConnectedRoom { get; }

		/// <summary>
		/// Requests the ping status with the quantum server
		/// </summary>
		IObservableFieldReader<bool> HasLag { get; }

		/// <summary>
		/// Requests the current quantum runner configs, from <see cref="IConfigsProvider"/>
		/// </summary>
		QuantumRunnerConfigs QuantumRunnerConfigs { get; }

		/// <summary>
		/// Load balancing client used to send/receive network ops, and get network callbacks.
		/// </summary>
		/// <remarks>Please do not call functions directly from this.
		/// <para>If needs be, implement them inside the service and call those.</para>
		/// <para>This can't be made private because it's used to add callback targets,
		/// has a lot of utils and useful code. Just don't abuse it, or you will regret it.</para></remarks>
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

		/// <summary>
		/// Last match room setup used to join or create rooms
		/// </summary>
		IObservableField<MatchRoomSetup> LastUsedSetup { get; }

		/// <summary>
		/// Set last connected room
		/// </summary>
		void SetLastRoom();
		
	}

	public enum JoinRoomSource
	{
		FirstJoin,
		ReconnectFrameSnapshot,
		RecreateFrameSnapshot,
		Reconnection
	}

	public static class SourceExt
	{
		public static bool HasResync(this JoinRoomSource src)
		{
			return src != JoinRoomSource.FirstJoin;
		}

		public static bool IsSnapshotAutoConnect(this JoinRoomSource src)
		{
			return src == JoinRoomSource.ReconnectFrameSnapshot || src == JoinRoomSource.RecreateFrameSnapshot;
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// This interface allows to manipulate the <see cref="IGameNetworkService"/> data.
	/// The goal for this interface separation is to allow <see cref="FirstLight.Game.StateMachines.NetworkState"/> to
	/// update the network data.
	/// </remarks>
	public interface IInternalGameNetworkService : IGameNetworkService
	{
		/// <inheritdoc cref="IGameNetworkService.UserId" />
		new IObservableField<string> UserId { get; }

		new IObservableField<JoinRoomSource> JoinSource { get; }

		/// <inheritdoc cref="IGameNetworkService.IsJoiningNewMatch" />
		new IObservableList<Player> LastMatchPlayers { get; }

		/// <inheritdoc cref="IGameNetworkService.LastDisconnectLocation" />
		new IObservableField<LastDisconnectionLocation> LastDisconnectLocation { get; }

		/// <inheritdoc cref="IGameNetworkService.LastConnectedRoomName" />
		new IObservableField<Room> LastConnectedRoom { get; }
	}

	/// <inheritdoc cref="IGameNetworkService"/>
	public class GameNetworkService : IInternalGameNetworkService
	{
		private const int LAG_RTT_THRESHOLD_MS = 280;
		private const int STORE_RTT_AMOUNT = 10;
		private const float QUANTUM_TICK_SECONDS = 0.25f;
		private const float QUANTUM_PING_TICK_SECONDS = 1f;

		private IConfigsProvider _configsProvider;
		private IGameDataProvider _dataProvider;
		private IGameServices _services;

		private Queue<int> LastRttQueue;
		private int CurrentRttTotal;
		private Coroutine _tickUpdateCoroutine;
		private Coroutine _tickPingCheckCoroutine;

		public IObservableField<string> UserId { get; }
		public IObservableField<JoinRoomSource> JoinSource { get; }
		public IObservableList<Player> LastMatchPlayers { get; }
		public IObservableField<LastDisconnectionLocation> LastDisconnectLocation { get; }
		public IObservableField<Room> LastConnectedRoom { get; }
		public QuantumLoadBalancingClient QuantumClient { get; }
		private IObservableField<bool> HasLag { get; }
		private IObservableField<MatchRoomSetup> LastUsedSetup { get; }

		string IGameNetworkService.UserId => UserId.Value;
		JoinRoomSource IGameNetworkService.JoinSource => JoinSource.Value;
		IObservableListReader<Player> IGameNetworkService.LastMatchPlayers => LastMatchPlayers;
		LastDisconnectionLocation IGameNetworkService.LastDisconnectLocation => LastDisconnectLocation.Value;
		Room IGameNetworkService.LastConnectedRoom => LastConnectedRoom.Value;
		IObservableFieldReader<bool> IGameNetworkService.HasLag => HasLag;
		IObservableField<MatchRoomSetup> IGameNetworkService.LastUsedSetup => LastUsedSetup;

		public Room CurrentRoom => QuantumClient.CurrentRoom;
		public Player LocalPlayer => QuantumClient.LocalPlayer;
		public bool InRoom => QuantumClient.InRoom;

		public QuantumRunnerConfigs QuantumRunnerConfigs => _configsProvider.GetConfig<QuantumRunnerConfigs>();

		public void SetLastRoom()
		{
			CurrentRoom.IsOffline = QuantumRunnerConfigs.IsOfflineMode;
			LastConnectedRoom.Value = CurrentRoom;
		}

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

				return _configsProvider.GetConfig<QuantumGameModeConfig>(QuantumClient.CurrentRoom.GetGameModeId());
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
		

		public GameNetworkService(IConfigsProvider configsProvider)
		{
			_configsProvider = configsProvider;

			QuantumClient = new QuantumLoadBalancingClient();
			JoinSource = new ObservableField<JoinRoomSource>(JoinRoomSource.FirstJoin);
			LastMatchPlayers = new ObservableList<Player>(new List<Player>());
			LastDisconnectLocation = new ObservableField<LastDisconnectionLocation>(LastDisconnectionLocation.None);
			LastConnectedRoom = new ObservableField<Room>(null);
			HasLag = new ObservableField<bool>(false);
			LastUsedSetup = new ObservableField<MatchRoomSetup>();
			UserId = new ObservableResolverField<string>(() => QuantumClient.UserId, SetUserId);
			LastRttQueue = new Queue<int>();
		}

		/// <summary>
		/// Binds services and data to the object, and starts starts ticking quantum client.
		/// Done here, instead of constructor because things are initialized in a particular order in Main.cs
		/// </summary>
		public void BindServicesAndData(IGameDataProvider dataProvider, IGameServices services)
		{
			_services = services;
			_dataProvider = dataProvider;
			
			_services.MessageBrokerService.Subscribe<PingedRegionsMessage>(OnPingRegions);
		}

		private void OnPingRegions(PingedRegionsMessage msg)
		{
			if (string.IsNullOrEmpty(_dataProvider.AppDataProvider.ConnectionRegion.Value))
			{
				_dataProvider.AppDataProvider.ConnectionRegion.Value = msg.RegionHandler.BestRegion.Code;
				_services.DataSaver.SaveData<AppData>();
				FLog.Info("Setting player default region to " + msg.RegionHandler.BestRegion.Code);
			}
		}

		public void EnableQuantumPingCheck(bool enabled)
		{
			if (_services == null) return;

			if (enabled)
			{
				_tickPingCheckCoroutine = _services.CoroutineService.StartCoroutine(TickPingCheck());
			}
			else
			{
				if (_tickPingCheckCoroutine != null)
				{
					_services.CoroutineService.StopCoroutine(_tickPingCheckCoroutine);
					_tickPingCheckCoroutine = null;
				}
			}
		}

		public void EnableClientUpdate(bool enabled)
		{
			if (_services == null) return;

			if (enabled)
			{
				_tickUpdateCoroutine = _services.CoroutineService.StartCoroutine(TickQuantumClient());
			}
			else
			{
				if (_tickUpdateCoroutine != null)
				{
					_services.CoroutineService.StopCoroutine(_tickUpdateCoroutine);
					_tickUpdateCoroutine = null;
				}
			}
		}

		private IEnumerator TickQuantumClient()
		{
			var waitForSeconds = new WaitForSeconds(QUANTUM_TICK_SECONDS);

			while (true)
			{
				if (QuantumClient.IsConnected && NetworkUtils.IsOnline())
				{
					QuantumClient.Service();
				}

				yield return waitForSeconds;
			}
		}

		private IEnumerator TickPingCheck()
		{
			var waitForSeconds = new WaitForSeconds(QUANTUM_PING_TICK_SECONDS);

			while (true)
			{
				yield return waitForSeconds;

				CalculateUpdateLag();
			}
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

			var bytesIn = QuantumClient.LoadBalancingPeer.BytesIn;
			var bytesOut = QuantumClient.LoadBalancingPeer.BytesOut;

			var roundTripCheck = RttAverage > LAG_RTT_THRESHOLD_MS;
			var dcCheck = NetworkUtils.IsOfflineOrDisconnected();

			HasLag.Value = roundTripCheck || dcCheck;
		}

		public bool ConnectPhotonServer()
		{
			FLog.Info("Connecting Photon Server");
			
			var settings = QuantumRunnerConfigs.PhotonServerSettings.AppSettings;
			if (QuantumClient.LoadBalancingPeer.PeerState == PeerStateValue.Connected && QuantumClient.Server == ServerConnection.NameServer)
			{
				if (settings.FixedRegion == null && !string.IsNullOrEmpty(_dataProvider.AppDataProvider.ConnectionRegion.Value))
				{
					FLog.Info("Server already in nameserver, connecting to master");
					ConnectPhotonToRegionMaster(_dataProvider.AppDataProvider.ConnectionRegion.Value);
					return true;
				}
			}
			
			if (QuantumClient.LoadBalancingPeer.PeerState != PeerStateValue.Disconnected)
			{
				FLog.Info("Not connecting photon due to status " + QuantumClient.LoadBalancingPeer.PeerState);
				return false;
			}

			
			if (!string.IsNullOrEmpty(_dataProvider.AppDataProvider.ConnectionRegion.Value))
			{
				FLog.Info("Connecting directly to master using region "+_dataProvider.AppDataProvider.ConnectionRegion.Value);
				settings.FixedRegion = _dataProvider.AppDataProvider.ConnectionRegion.Value;
			}
			else
			{
				FLog.Info("Connecting to nameserver without region to detect best region");
				settings.FixedRegion = null;
			}
			ResetQuantumProperties();

			return QuantumClient.ConnectUsingSettings(settings, _dataProvider.AppDataProvider.DisplayNameTrimmed);
		}

		public bool ConnectPhotonToRegionMaster(string region)
		{
			FLog.Verbose("Connected to Region " + region);
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
			FLog.Info($"JoinRoom: {InRoom}");

			if (InRoom) return false;

			var enterParams = NetworkUtils.GetRoomEnterParams(roomName);

			QuantumRunnerConfigs.IsOfflineMode = false;

			ResetQuantumProperties();
			SetSpectatePlayerProperty(false);
			LastUsedSetup.Value = null;

			return QuantumClient.OpJoinRoom(enterParams);
		}

		public bool CreateRoom(MatchRoomSetup setup, bool offlineMode)
		{
			if (InRoom) return false;

			FLog.Info($"CreateRoom: {setup}");

			var createParams = NetworkUtils.GetRoomCreateParams(setup, NetworkUtils.GetRandomDropzonePosRot());

			QuantumRunnerConfigs.IsOfflineMode = offlineMode;

			ResetQuantumProperties();
			SetSpectatePlayerProperty(false);
			LastDisconnectLocation.Value = LastDisconnectionLocation.None;
			LastUsedSetup.Value = setup;
			return QuantumClient.OpCreateRoom(createParams);
		}


		public bool JoinOrCreateRoom(MatchRoomSetup setup, string teamID = null, string[] expectedPlayers = null)
		{
			if (InRoom) return false;

			FLog.Info($"JoinOrCreateRoom: {setup}");

			var createParams =
				NetworkUtils.GetRoomCreateParams(setup, NetworkUtils.GetRandomDropzonePosRot(), expectedPlayers);

			QuantumRunnerConfigs.IsOfflineMode = false;

			ResetQuantumProperties(teamID);
			SetSpectatePlayerProperty(false);
			LastDisconnectLocation.Value = LastDisconnectionLocation.None;
			LastUsedSetup.Value = setup;
			return QuantumClient.OpJoinOrCreateRoom(createParams);
		}

		public bool RejoinRoom(string room)
		{
			if (InRoom) return false;

			FLog.Info($"RejoinRoom: {room}");

			QuantumRunnerConfigs.IsOfflineMode = false;
			LastDisconnectLocation.Value = LastDisconnectionLocation.None;
			return QuantumClient.OpRejoinRoom(room);
		}

		public bool JoinOrCreateRandomRoom(MatchRoomSetup setup)
		{
			if (InRoom) return false;

			FLog.Info($"JoinOrCreateRandomRoom: {setup}");

			// On random room we always send vector zero as spawn position
			// this is to ensure all rooms are created with same properties for all players so
			// quantum can matchmake em safely when concurrency happens
			var createParams = NetworkUtils.GetRoomCreateParams(setup, Vector3.zero);
			var joinRandomParams = NetworkUtils.GetJoinRandomRoomParams(setup);

			QuantumRunnerConfigs.IsOfflineMode = false;

			ResetQuantumProperties();

			SetSpectatePlayerProperty(false);
			LastDisconnectLocation.Value = LastDisconnectionLocation.None;
			LastUsedSetup.Value = setup;

			return QuantumClient.OpJoinRandomOrCreateRoom(joinRandomParams, createParams);
		}

		public bool LeaveRoom(bool becomeInactive, bool sendAuthCookie)
		{
			if (!InRoom) return false;

			FLog.Info("LeaveRoom");

			return QuantumClient.OpLeaveRoom(becomeInactive, true);
		}

		public bool SendPlayerToken(string token)
		{
			var opt = new RaiseEventOptions {Receivers = ReceiverGroup.All};
			return QuantumClient.OpRaiseEvent((int) QuantumCustomEvents.Token, Encoding.UTF8.GetBytes(token), opt,
				SendOptions.SendReliable);
		}

		public bool KickPlayer(Player playerToKick)
		{
			if (CurrentRoom == null || !LocalPlayer.IsMasterClient)
			{
				return false;
			}

			FLog.Info($"KickPlayer: {playerToKick}");

			var eventOptions = new RaiseEventOptions() {Receivers = ReceiverGroup.All};
			return QuantumClient.OpRaiseEvent((byte) QuantumCustomEvents.KickPlayer, playerToKick.ActorNumber,
				eventOptions,
				SendOptions.SendReliable);
		}

		public void ReconnectPhoton(out bool requiresManualReconnection)
		{
			FLog.Info("ReconnectPhoton");

			requiresManualReconnection = false;
			JoinSource.Value = JoinRoomSource.Reconnection;
			
			if (QuantumClient.LoadBalancingPeer.PeerState != PeerStateValue.Disconnected) return;

			if (QuantumClient.Server == ServerConnection.GameServer)
			{
				FLog.Info("ReconnectPhoton - ReconnectAndRejoin");
				QuantumClient.ReconnectAndRejoin();
			}
			else
			{
				FLog.Info("ReconnectPhoton - ReconnectToMaster");
				QuantumClient.ReconnectToMaster();
			}
		}

		public void SetManualTeamId(string teamId)
		{
			var playerPropsUpdate = new Hashtable
			{
				{
					GameConstants.Network.PLAYER_PROPS_TEAM_ID, teamId
				}
			};

			SetPlayerCustomProperties(playerPropsUpdate);
		}

		public void SetDropPosition(Vector2 dropPosition)
		{
			var playerPropsUpdate = new Hashtable
			{
				{
					GameConstants.Network.PLAYER_PROPS_DROP_POSITION, dropPosition
				}
			};
			SetPlayerCustomProperties(playerPropsUpdate);
		}

		public void SetCurrentRoomOpen(bool isOpen)
		{
			FLog.Verbose("Setting room open: "+isOpen);
			CurrentRoom.IsOpen = isOpen;
		}

		public void SetPlayerCustomProperties(Hashtable propertiesToUpdate)
		{
			FLog.Verbose("Setting player properties");
			FLog.Verbose(propertiesToUpdate);
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

		private void ResetQuantumProperties(string teamId = null)
		{
			if (QuantumClient.AuthValues != null)
			{
				QuantumClient.AuthValues.AuthType = CustomAuthenticationType.Custom;
			}
			QuantumClient.EnableProtocolFallback = true;
			QuantumClient.NickName = _dataProvider.AppDataProvider.DisplayNameTrimmed;
			var preloadIds = new List<int>();

			if (_dataProvider.EquipmentDataProvider.Loadout != null)
			{
				foreach (var item in _dataProvider.EquipmentDataProvider.Loadout)
				{
					var equipmentDataInfo = _dataProvider.EquipmentDataProvider.Inventory[item.Value];
					preloadIds.Add((int) equipmentDataInfo.GameId);
				}

				preloadIds.Add((int) _dataProvider.CollectionDataProvider.GetEquipped(new(GameIdGroup.PlayerSkin)).Id);
			}

			var playerProps = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_LOADOUT, preloadIds.ToArray()},
				{GameConstants.Network.PLAYER_PROPS_CORE_LOADED, false},
				{GameConstants.Network.PLAYER_PROPS_SPECTATOR, false},
				{GameConstants.Network.PLAYER_PROPS_TEAM_ID, teamId},
				{GameConstants.Network.PLAYER_PROPS_RANK, _services.LeaderboardService.CurrentRankedEntry.Position},
			};

			SetPlayerCustomProperties(playerProps);
		}
	}
}
