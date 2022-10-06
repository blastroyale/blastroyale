using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Photon.Realtime;
using Quantum;

namespace FirstLight.Game.Services
{
	public enum LastDisconnectionLocation
	{
		None,
		Menu,
		Matchmaking,
		Loading,
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
		private const int _lagRoundtripThreshold = 500; // yellow > 200
		
		private IConfigsProvider _configsProvider;
		private bool _isJoiningNewRoom;
		
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

		public GameNetworkService(IConfigsProvider configsProvider)
		{
			_configsProvider = configsProvider;
			QuantumClient = new QuantumLoadBalancingClient();
			IsJoiningNewMatch = new ObservableField<bool>(false);
			LastMatchPlayers = new ObservableList<Player>(new List<Player>());
			LastDisconnectLocation = new ObservableField<LastDisconnectionLocation>(LastDisconnectionLocation.None);
			LastConnectedRoomName = new ObservableField<string>("");
			HasLag = new ObservableField<bool>(false);
			UserId = new ObservableResolverField<string>(() => QuantumClient.UserId, SetUserId);
		}

		/// <inheritdoc />
		public void CheckLag()
		{
			/*var lastTimestamp = QuantumClient.LoadBalancingPeer.TimestampOfLastSocketReceive;
			var roundTripCheck = QuantumClient.LoadBalancingPeer.LastRoundTripTime > _lagRoundtripThreshold;
			var lastAckCheck = lastTimestamp > 0 && SupportClass.GetTickCount() - lastTimestamp > _lagRoundtripThreshold;
			
			HasLag.Value = roundTripCheck || lastAckCheck;*/
		}

		private void SetUserId(string id)
		{
			QuantumClient.UserId = id;
			QuantumClient.AuthValues.AuthGetParameters = "";

			QuantumClient.AuthValues.AddAuthParameter("username", id);
		}
	}
}