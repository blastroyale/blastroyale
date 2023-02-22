using Photon.Deterministic;

namespace Quantum.Systems
{
	public unsafe class TriggerSystem: SystemSignalsOnly, ISignalPlayerDead, ISignalChestOpened
	{
		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			var playersAlive = AlivePlayerCount(f);
			foreach (var pair in f.Unsafe.GetComponentBlockIterator<Trigger>())
			{
				var trigger = pair.Component;

				if (pair.Component->Data.Field == TriggerData.PLAYERSALIVETRIGGERDATA && playersAlive <= trigger->Data.PlayersAliveTriggerData->PlayersAlive)
				{
					f.Signals.TriggerActivated(trigger->Target, trigger->Data);
				}
			}
		}
		
		public void ChestOpened(Frame f, GameId chestType, FPVector3 chestPosition, PlayerRef player, EntityRef entity)
		{
			foreach (var pair in f.Unsafe.GetComponentBlockIterator<Trigger>())
			{
				var trigger = pair.Component;
				if (pair.Component->Data.Field == TriggerData.CHESTOPENTRIGGERDATA)
				{
					f.Signals.TriggerActivated(trigger->Target, trigger->Data);
				}
			}
		}

		private int AlivePlayerCount(Frame f)
		{
			return f.ComponentCount<AlivePlayerCharacter>();
		}
	}
}