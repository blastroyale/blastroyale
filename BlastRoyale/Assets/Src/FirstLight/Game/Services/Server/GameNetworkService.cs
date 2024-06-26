using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Photon.Realtime;
using UnityEngine;
using Quantum;
using Unity.Services.UserReporting;
using Unity.Services.Authentication;
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
		/// Sends user token to Quantum Server to prove the user is authenticated and able to send commands.
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool SendPlayerToken(string token);

		/// <summary>
		/// Sets the current room <see cref="Room.IsOpen"/> property, which sets whether it can be joined or not
		/// </summary>
		void SetCurrentRoomOpen(bool isOpen);

		/// <summary>
		/// Updates/Adds Photon LocalPlayer custom properties
		/// </summary>
		void SetPlayerCustomProperties(Hashtable propertiesToUpdate);

		/// <summary>
		/// Requests the current room that the local player is in
		/// </summary>
		Photon.Realtime.Room CurrentRoom { get; }

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

		/// <summary>
		/// Requests the check if the last disconnection was in matchmaking, before the match started
		/// </summary>
		LastDisconnectionLocation LastDisconnectLocation { get; set; }

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
		/// Last match room setup used to join or create rooms
		/// </summary>
		IObservableField<MatchRoomSetup> LastUsedSetup { get; }

		/// <summary>
		/// Photon server time in miliseconds
		/// </summary>
		/// 
		int ServerTimeInMilliseconds { get; }

		/// <summary>
		/// Event for when quantum client connects to master
		/// </summary>
		public event Action OnConnectedToMaster;

		/// <summary>
		/// Set last connected room
		/// </summary>
		void SetLastRoom();

		/// <summary>
		/// Awaits until server connection is stablished
		/// Will return false if connection could be made
		/// </summary>
		UniTask<bool> AwaitMasterServerConnection(int timeout = 10, string server = "");

		void ChangeServerRegionAndReconnect(string regionCode);
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

		/// <inheritdoc cref="IGameNetworkService.LastDisconnectLocation" />
		new IObservableField<LastDisconnectionLocation> LastDisconnectLocation { get; }

		/// <inheritdoc cref="IGameNetworkService.LastConnectedRoomName" />
		new IObservableField<Room> LastConnectedRoom { get; }
	}

	/// <inheritdoc cref="IGameNetworkService"/>
	public class GameNetworkService : IInternalGameNetworkService, IConnectionCallbacks
	{
		private const int LAG_RTT_THRESHOLD_MS = 140;
		private const int STORE_RTT_AMOUNT = 10;
		private const float QUANTUM_TICK_SECONDS = 0.25f;
		private const float QUANTUM_PING_TICK_SECONDS = 1f;

		private IConfigsProvider _configsProvider;
		private IGameDataProvider _dataProvider;
		private IGameServices _services;

		private Queue<int> LastRttQueue;
		private int CurrentRttTotal;
		private Coroutine _tickPingCheckCoroutine;
		private bool _ticking = false;

		public IObservableField<string> UserId { get; }
		public IObservableField<JoinRoomSource> JoinSource { get; }
		public IObservableField<LastDisconnectionLocation> LastDisconnectLocation { get; }
		public IObservableField<Room> LastConnectedRoom { get; }
		public QuantumLoadBalancingClient QuantumClient { get; }
		private IObservableField<bool> HasLag { get; }
		private IObservableField<MatchRoomSetup> LastUsedSetup { get; }
		public int ServerTimeInMilliseconds => QuantumClient.LoadBalancingPeer.ServerTimeInMilliSeconds;
		string IGameNetworkService.UserId => UserId.Value;
		JoinRoomSource IGameNetworkService.JoinSource => JoinSource.Value;

		LastDisconnectionLocation IGameNetworkService.LastDisconnectLocation
		{
			get => LastDisconnectLocation.Value;
			set => LastDisconnectLocation.Value = value;
		}

		Room IGameNetworkService.LastConnectedRoom => LastConnectedRoom.Value;
		IObservableFieldReader<bool> IGameNetworkService.HasLag => HasLag;
		IObservableField<MatchRoomSetup> IGameNetworkService.LastUsedSetup => LastUsedSetup;

		public Room CurrentRoom => QuantumClient.CurrentRoom;
		public Player LocalPlayer => QuantumClient.LocalPlayer;
		public bool InRoom => QuantumClient.InRoom;

		public event Action OnConnectedToMaster;

		public QuantumRunnerConfigs QuantumRunnerConfigs => _configsProvider.GetConfig<QuantumRunnerConfigs>();

		public void SetLastRoom()
		{
			CurrentRoom.IsOffline = QuantumRunnerConfigs.IsOfflineMode;
			LastConnectedRoom.Value = CurrentRoom;
		}

		public async UniTask<bool> AwaitMasterServerConnection(int timeout = 10, string server = "")
		{
			var i = timeout * 4;
			while (i > 0)
			{
				if (QuantumClient.IsConnectedAndReady)
				{
					if (string.IsNullOrEmpty(server) || QuantumClient.CloudRegion == server)
					{
						return true;
					}
				}

				await UniTask.Delay(250);
				if (Time.timeScale == 0) QuantumClient.Service();
				i--;
			}

			return false;
		}

		private int RttAverage => CurrentRttTotal / LastRttQueue.Count;

		public GameNetworkService(IConfigsProvider configsProvider)
		{
			_configsProvider = configsProvider;

			QuantumClient = new QuantumLoadBalancingClient();
			JoinSource = new ObservableField<JoinRoomSource>(JoinRoomSource.FirstJoin);
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
		public void StartNetworking(IGameDataProvider dataProvider, IGameServices services)
		{
			_services = services;
			_dataProvider = dataProvider;

			_services.MessageBrokerService.Subscribe<PingedRegionsMessage>(OnPingRegions);
			_services.TickService.SubscribeOnUpdate(QuantumTick);
			_ticking = true;

			QuantumCallback.SubscribeManual<CallbackGameDestroyed>(this, OnSimulationFinish);
			QuantumCallback.SubscribeManual<CallbackGameStarted>(this, OnSimulationStarted);
		}

		private void OnSimulationFinish(CallbackGameDestroyed cb)
		{
			if (!_ticking)
			{
				_services.TickService.SubscribeOnUpdate(QuantumTick);
				_ticking = true;
				FLog.Info("Quantum Tick = true");
			}
		}

		private void OnSimulationStarted(CallbackGameStarted cb)
		{
			var isOffline = _services.RoomService.CurrentRoom?.IsOffline ?? false;
			if (isOffline)
			{
				return;
			}

			if (_ticking)
			{
				_services.TickService.UnsubscribeOnUpdate(QuantumTick);
				_ticking = false;
				FLog.Info("Quantum Tick = false");
			}
		}

		private void QuantumTick(float f)
		{
			// We should always tick during offline simulations, otherwise the connection to quantum will timeout
			var isOffline = _services.RoomService.CurrentRoom?.IsOffline ?? false;
			if (!isOffline && QuantumRunner.Default.IsDefinedAndRunning(false)) return;
			QuantumClient.Service();
			UserReportingService.Instance.SampleMetric("Quantum.LastRoundTripTime", QuantumClient.LoadBalancingPeer.LastRoundTripTime);
		}

		private void OnPingRegions(PingedRegionsMessage msg)
		{
			if (string.IsNullOrEmpty(_services.LocalPrefsService.ServerRegion.Value))
			{
				_services.LocalPrefsService.ServerRegion.Value = msg.RegionHandler.BestRegion.Code;
				FLog.Info("Setting player default region to " + msg.RegionHandler.BestRegion.Code);
			}
		}

		//[Conditional("DEBUG")]
		private void DebugConnection()
		{
			FLog.Verbose("Connection", $"State = {QuantumClient.State.ToString()}");
			FLog.Verbose("Connection", $"Peer State = {QuantumClient.LoadBalancingPeer.PeerState.ToString()}");
			FLog.Verbose("Connection", $"Server = {QuantumClient.Server.ToString()}");
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
			var newRtt = QuantumClient.LoadBalancingPeer.LastRoundTripTime / 2;
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

		public void ChangeServerRegionAndReconnect(string serverCode)
		{
			_services.LocalPrefsService.ServerRegion.Value = serverCode;
			_services.DataService.SaveData<AppData>();
			DisconnectPhoton();
			FLog.Info("Changing region to " + serverCode);
		}

		public bool ConnectPhotonServer()
		{
			FLog.Info("Connecting Photon Server");

			var settings = QuantumRunnerConfigs.PhotonServerSettings.AppSettings;
			if (QuantumClient.LoadBalancingPeer.PeerState == PeerStateValue.Connected && QuantumClient.Server == ServerConnection.NameServer)
			{
				if (settings.FixedRegion == null && !string.IsNullOrEmpty(_services.LocalPrefsService.ServerRegion.Value))
				{
					FLog.Info("Server already in nameserver, connecting to master");
					ConnectPhotonToRegionMaster(_services.LocalPrefsService.ServerRegion.Value);
					return true;
				}
			}

			if (QuantumClient.LoadBalancingPeer.PeerState != PeerStateValue.Disconnected)
			{
				FLog.Info("Not connecting photon due to status " + QuantumClient.LoadBalancingPeer.PeerState);
				return false;
			}


			if (!string.IsNullOrEmpty(_services.LocalPrefsService.ServerRegion.Value))
			{
				FLog.Info("Connecting directly to master using region " + _services.LocalPrefsService.ServerRegion.Value);
				settings.FixedRegion = _services.LocalPrefsService.ServerRegion.Value;
			}
			else
			{
				FLog.Info("Connecting to nameserver without region to detect best region");
				settings.FixedRegion = null;
			}

			ResetQuantumProperties();

			return QuantumClient.ConnectUsingSettings(settings, AuthenticationService.Instance.GetPlayerName());
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

		public bool SendPlayerToken(string token)
		{
			var opt = new RaiseEventOptions {Receivers = ReceiverGroup.All};
			return QuantumClient.OpRaiseEvent((int) QuantumCustomEvents.Token, Encoding.UTF8.GetBytes(token), opt,
				SendOptions.SendReliable);
		}


		public void ReconnectPhoton(out bool requiresManualReconnection)
		{
			requiresManualReconnection = false;
			JoinSource.Value = JoinRoomSource.Reconnection;

			if (QuantumClient.LoadBalancingPeer.PeerState != PeerStateValue.Disconnected) return;

			FLog.Info("ReconnectPhoton");

			if (QuantumClient.Server == ServerConnection.GameServer)
			{
				FLog.Info("ReconnectPhoton - ReconnectAndRejoin");
				QuantumClient.ReconnectAndRejoin();
			}
			else
			{
				var settingsRegion = _services.LocalPrefsService.ServerRegion.Value;
				if (settingsRegion != QuantumClient.CloudRegion)
				{
					FLog.Info("ReconnectPhoton - ReconnectToMaster changing region ");
					QuantumClient.ConnectToRegionMaster(settingsRegion);
					return;
				}

				FLog.Info("ReconnectPhoton - ReconnectToMaster");
				QuantumClient.ReconnectToMaster();
			}
		}


		public void SetCurrentRoomOpen(bool isOpen)
		{
			FLog.Verbose("Setting room open: " + isOpen);
			CurrentRoom.IsOpen = isOpen;
		}

		public void SetPlayerCustomProperties(Hashtable propertiesToUpdate)
		{
			FLog.Verbose("Setting player properties");
			FLog.Verbose(propertiesToUpdate);
			QuantumClient.LocalPlayer.SetCustomProperties(propertiesToUpdate);
		}


		private void SetUserId(string id)
		{
			QuantumClient.UserId = id;
			QuantumClient.AuthValues.AuthGetParameters = "";
			QuantumClient.AuthValues.AddAuthParameter("username", id);
		}


		private void ResetQuantumProperties()
		{
			if (QuantumClient.AuthValues != null)
			{
				QuantumClient.AuthValues.AuthType = CustomAuthenticationType.Custom;
			}

			QuantumClient.EnableProtocolFallback = true;
		}

		#region IConnectionCallbacks

		public void OnConnected()
		{
		}

		void IConnectionCallbacks.OnConnectedToMaster()
		{
			OnConnectedToMaster?.Invoke();
		}

		public void OnDisconnected(DisconnectCause cause)
		{
		}

		public void OnRegionListReceived(RegionHandler regionHandler)
		{
		}

		public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
		{
		}

		public void OnCustomAuthenticationFailed(string debugMessage)
		{
		}

		#endregion
	}
}
