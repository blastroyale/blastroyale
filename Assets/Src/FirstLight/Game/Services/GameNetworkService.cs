using ExitGames.Client.Photon;
using FirstLight.Game.Infos;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Utils;
using Photon.Realtime;
using PlayFab;
using Quantum;

namespace FirstLight.Game.Services
{
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
		bool IsJoiningNewRoom { get; }

		/// <summary>
		/// Requests the ping status with the quantum server
		/// </summary>
		IObservableFieldReader<bool> HasLag { get; }
		
		/// <inheritdoc cref="QuantumLoadBalancingClient" />
		QuantumLoadBalancingClient QuantumClient { get; }
		
		/// <summary>
		/// Requests the current <see cref="MapConfig"/> for the map set on the current connected room.
		/// If the player is not connected to any room then it return NULL without a value
		/// </summary>
		QuantumMapConfig? CurrentRoomMapConfig { get; }

		/// <summary>
		/// Requests to see if the <paramref name="room"/> is set for matchmaking
		/// </summary>
		bool IsMatchmakingRoom(Room room);
		
		/// <summary>
		/// Requests to check if the current room in QuantumClient is set for matchmaking
		/// </summary>
		bool IsCurrentRoomForMatchmaking { get; }
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
		
		/// <inheritdoc cref="IGameNetworkService.IsJoiningNewRoom" />
		new IObservableField<bool> IsJoiningNewRoom { get; }
		
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
		bool IGameNetworkService.IsJoiningNewRoom => IsJoiningNewRoom.Value;
		public IObservableField<bool> IsJoiningNewRoom { get; }
		string IGameNetworkService.UserId => UserId.Value;
		IObservableFieldReader<bool> IGameNetworkService.HasLag => HasLag;
		
		public QuantumLoadBalancingClient QuantumClient { get; }
		public bool IsCurrentRoomForMatchmaking => IsMatchmakingRoom(QuantumClient.CurrentRoom);

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

		public bool IsMatchmakingRoom(Room room)
		{
			return room.IsVisible;
		}

		private IObservableField<bool> HasLag { get; }

		public GameNetworkService(IConfigsProvider configsProvider)
		{
			_configsProvider = configsProvider;
			QuantumClient = new QuantumLoadBalancingClient();
			IsJoiningNewRoom = new ObservableField<bool>(false);
			HasLag = new ObservableField<bool>(false);
			UserId = new ObservableResolverField<string>(() => QuantumClient.UserId, SetUserId);
			UserId.Value = PlayFabSettings.DeviceUniqueIdentifier;
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