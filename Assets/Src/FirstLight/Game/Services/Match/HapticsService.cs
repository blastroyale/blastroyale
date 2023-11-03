using Lofelt.NiceVibrations;
using Quantum;

namespace FirstLight.Game.Services.Match
{
	public interface IHapticsService
	{
	}

	public class HapticsService : IHapticsService, MatchServices.IMatchService
	{
		private readonly IGameServices _gameServices;
		private readonly IMatchServices _matchServices;

		public HapticsService(IGameServices gameServices, IMatchServices matchServices)
		{
			_gameServices = gameServices;
			_matchServices = matchServices;

			QuantumEvent.SubscribeManual<EventOnPlayerAttackHit>(this, OnPlayerAttackHit);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
		}

		private void OnPlayerAttackHit(EventOnPlayerAttackHit callback)
		{
			if (callback.PlayerEntity != _matchServices.SpectateService.GetSpectatedEntity()) return;

			HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.EntityDead == _matchServices.SpectateService.GetSpectatedEntity())
			{
				HapticPatterns.PlayPreset(HapticPatterns.PresetType.Failure);
			}
			else if (callback.EntityKiller == _matchServices.SpectateService.GetSpectatedEntity())
			{
				HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
			}
		}

		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
		}
	}
}