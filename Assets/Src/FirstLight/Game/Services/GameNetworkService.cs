using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExitGames.Client.Photon;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Photon.Realtime;
using Quantum;
using Debug = UnityEngine.Debug;

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
		/// <returns>True if the connect operation was successful (not status of whether you are connected) </returns>
		bool ConnectPhotonToMaster();
		
		/// <summary>
		/// Connects Photon to a specific region master server
		/// </summary>
		/// <returns>True if the connect operation was successful (not status of whether you are connected) </returns>
		bool ConnectPhotonToRegionMaster(string region);
		
		/// <summary>
		/// Connects Photon to the the name server
		/// NOTE: You must disconnect from master serer before connecting to the name server
		/// </summary>
		/// <returns>True if the connect operation was successful (not status of whether you are connected) </returns>
		bool ConnectPhotonToNameServer();

		/// <summary>
		/// Reconnects photon in the most suitable way, based on parameters, after a user was disconnected
		/// </summary>
		/// <param name="inMatchScene">Set true if last disconnetion occured on match scene</param>
		/// <param name="requiresManualReconnection">This will be true if disconnected during matchmaking
		/// because during matchmaking, TTL is 0 - disconnected user is booted out of the room.</param>
		void ReconnectPhoton(bool inMatchScene, out bool requiresManualReconnection);
		
		/// <summary>
		/// Disconnects Photon from whatever server it's currently connected to
		/// </summary>
		void DisconnectPhoton();

		/// <summary>
		/// Updates/Adds Photon LocalPlayer custom properties
		/// </summary>
		/// <param name="propertiesToUpdate">Hashtable of properties to add/update for the player</param>
		void UpdatePlayerCustomProperties(Hashtable propertiesToUpdate);
		
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
		QuantumLoadBalancingClient QuantumClient { get; }
		
		/// <summary>
		/// Requests the information if the current client is a spectator player just watching the match
		/// </summary>
		bool IsSpectorPlayer { get; }

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
		
		/// <summary>
		/// Checks if the current frame is having connections issues and if it is lagging
		/// </summary>
		void CheckLag();
	}
	
	/// <inheritdoc cref="IGameNetworkService"/>
	public class GameNetworkService : IGameBackendNetworkService
	{
		private const int LAG_RTT_THRESHOLD_MS = 280;
		private const int STORE_RTT_AMOUNT = 10;
		
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
		public bool IsSpectorPlayer => QuantumClient.LocalPlayer.IsSpectator();
		private IObservableField<bool> HasLag { get; }
		
		string IGameNetworkService.UserId => UserId.Value;
		bool IGameNetworkService.IsJoiningNewMatch => IsJoiningNewMatch.Value;
		IObservableListReader<Player> IGameNetworkService.LastMatchPlayers => LastMatchPlayers;
		LastDisconnectionLocation IGameNetworkService.LastDisconnectLocation => LastDisconnectLocation.Value;
		string IGameNetworkService.LastConnectedRoomName => LastConnectedRoomName.Value;
		IObservableFieldReader<bool> IGameNetworkService.HasLag => HasLag;
		
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

				return _configsProvider.GetConfig<QuantumGameModeConfig>(QuantumClient.CurrentRoom.GetGameModeId().GetHashCode());
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

		public GameNetworkService(IConfigsProvider configsProvider, IGameDataProvider gameDataProvider, IGameServices gameServices)
		{
			_configsProvider = configsProvider;
			QuantumClient = new QuantumLoadBalancingClient();
			IsJoiningNewMatch = new ObservableField<bool>(false);
			LastMatchPlayers = new ObservableList<Player>(new List<Player>());
			LastDisconnectLocation = new ObservableField<LastDisconnectionLocation>(LastDisconnectionLocation.None);
			LastConnectedRoomName = new ObservableField<string>("");
			HasLag = new ObservableField<bool>(false);
			UserId = new ObservableResolverField<string>(() => QuantumClient.UserId, SetUserId);
			LastRttQueue = new Queue<int>();
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
		
		public void UpdatePlayerCustomProperties(Hashtable propertiesToUpdate)
		{
			QuantumClient.LocalPlayer.SetCustomProperties(propertiesToUpdate);
		}
		
		public void SetSpectatePlayerProperty(bool isSpectator)
		{
			var playerPropsUpdate = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_SPECTATOR, isSpectator}
			};

			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}
		
		public void CheckLag()
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