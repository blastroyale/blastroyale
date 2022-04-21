using ExitGames.Client.Photon;
using FirstLight.Game.Infos;
using FirstLight;
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
		/// Requests the ping status with the quantum server
		/// </summary>
		IObservableFieldReader<bool> HasLag { get; }
		
		/// <inheritdoc cref="QuantumLoadBalancingClient" />
		QuantumLoadBalancingClient QuantumClient { get; }
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
		
		/// <summary>
		/// Checks if the current frame is having connections issues and if it is lagging
		/// </summary>
		void CheckLag();
	}
	
	/// <inheritdoc cref="IGameNetworkService"/>
	public class GameNetworkService : IGameBackendNetworkService
	{
		private const int _lagRoundtripThreshold = 500; // yellow > 200

		/// <inheritdoc />
		string IGameNetworkService.UserId => UserId.Value;
		
		/// <inheritdoc />
		IObservableFieldReader<bool> IGameNetworkService.HasLag => HasLag;

		/// <inheritdoc />
		public IObservableField<string> UserId { get; }
		/// <inheritdoc />
		public QuantumLoadBalancingClient QuantumClient { get; }

		private IObservableField<bool> HasLag { get; }

		public GameNetworkService()
		{
			QuantumClient = new QuantumLoadBalancingClient();
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