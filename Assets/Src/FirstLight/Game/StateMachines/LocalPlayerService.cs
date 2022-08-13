using FirstLight.Game.Services;
using Quantum;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This service provides the necessary API to help the match control and access all local player behaviour
	/// </summary>
	public interface ILocalPlayerService
	{
		
	}
	
	public class LocalPlayerService : ILocalPlayerService, MatchServices.IMatchService
	{
		private readonly IGameServices _gameServices;
		private readonly IMatchServices _matchServices;

		public LocalPlayerService(IGameServices gameServices, IMatchServices matchServices)
		{
			_gameServices = gameServices;
			_matchServices = matchServices;
		}

		public void Dispose()
		{
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
		}
		
		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			throw new System.NotImplementedException();
		}

		public void OnMatchEnded()
		{
			throw new System.NotImplementedException();
		}
	}
}