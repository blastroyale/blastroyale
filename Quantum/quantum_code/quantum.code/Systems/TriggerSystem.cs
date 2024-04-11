using Photon.Deterministic;

namespace Quantum.Systems
{
	public unsafe class TriggerSystem: SystemSignalsOnly, ISignalPlayerDead, ISignalChestOpened, ISignalCollectableCollected
	{
		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			var playersAlive = AlivePlayerCount(f);
			foreach (var pair in f.Unsafe.GetComponentBlockIterator<Trigger>())
			{
				var trigger = pair.Component;

				if (pair.Component->Data.Field == TriggerData.PLAYERSALIVETRIGGERDATA && playersAlive <= trigger->Data.PlayersAliveTriggerData->PlayersAlive)
				{
					SendTriggerActivated(f, trigger->Target, trigger->Data);
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
					SendTriggerActivated(f, trigger->Target, trigger->Data);
				}
			}
		}
		
		
		public void CollectableCollected(Frame f, GameId collectableId, EntityRef collectableEntity,
										 EntityRef collectorEntity, EntityRef spawner)
		{
			if (collectableId.IsInGroup(GameIdGroup.Weapon))
			{
				foreach (var pair in f.Unsafe.GetComponentBlockIterator<Trigger>())
				{
					var trigger = pair.Component;

					if (pair.Component->Data.Field == TriggerData.WEAPONCOLLECTEDTRIGGERDATA && pair.Component->Data.WeaponCollectedTriggerData->WeaponSpawner == spawner)
					{
						SendTriggerActivated(f, trigger->Target, trigger->Data);
					}
				}
			}
		}

		private void SendTriggerActivated(Frame f, EntityRef target, TriggerData data)
		{
			f.Signals.TriggerActivated(target, data);
			f.Events.OnTriggerActivated(target);
		}

		private int AlivePlayerCount(Frame f)
		{
			return f.ComponentCount<AlivePlayerCharacter>();
		}


	}
}