using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Lofelt.NiceVibrations;
using Quantum;

namespace FirstLight.Game.Services.Match
{
	public interface IHapticsService
	{
	}

	public class HapticsService : IHapticsService, MatchServices.IMatchService
	{
		private EntityRef _localPlayerEntity;

		public HapticsService(IGameDataProvider dataProvider)
		{
			if (!dataProvider.AppDataProvider.IsHapticOn) return;

			QuantumEvent.SubscribeManual<EventOnPlayerAttackHit>(this, OnPlayerAttackHit);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnPlayerKnockedOut>(this, OnPlayerKnockedOut);
			QuantumEvent.SubscribeManual<EventOnAllPlayersJoined>(this, OnAllPlayersJoined);
		}

		private void OnAllPlayersJoined(EventOnAllPlayersJoined callback)
		{
			_localPlayerEntity = callback.Game.GetLocalPlayerData(true, out _).Entity;
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			// only handles reconnection
			if (!isReconnect) return;
			_localPlayerEntity = game.GetLocalPlayerData(true, out _).Entity;
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
		}

		private void OnPlayerAttackHit(EventOnPlayerAttackHit callback)
		{
			if (callback.PlayerEntity != _localPlayerEntity) return;
			if (callback.SpellType == Spell.KnockedOut) return;
			HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
		}

		private void OnPlayerKnockedOut(EventOnPlayerKnockedOut callback)
		{
			if (callback.Entity == _localPlayerEntity)
			{
				HapticPatterns.PlayPreset(HapticPatterns.PresetType.Failure);
			}
			else if (callback.Attacker == _localPlayerEntity)
			{
				HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
			}
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.EntityDead == _localPlayerEntity)
			{
				HapticPatterns.PlayPreset(HapticPatterns.PresetType.Failure);
			}
			else if (callback.EntityKiller == _localPlayerEntity)
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