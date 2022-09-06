using Photon.Deterministic;

namespace Quantum.Systems
{
	public unsafe class PlayerCombatEventsSystem : SystemSignalsOnly, ISignalOnPlayerDataSet, ISignalPlayerKilledPlayer,
	                                               ISignalPlayerDead
	{
		public void OnPlayerDataSet(Frame f, PlayerRef player)
		{
			var data = f.GetPlayerData(player);
		}

		public void PlayerKilledPlayer(Frame f, PlayerRef playerDead, EntityRef entityDead, PlayerRef playerKiller,
		                               EntityRef entityKiller)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();

			if (playerDead != playerKiller)
			{
				var killerData = gameContainer->PlayersData.GetPointer(playerKiller);

				killerData->CurrentKillStreak++;

				if (f.Time <= killerData->MultiKillResetTime)
				{
					killerData->CurrentMultiKill++;
				}
				else
				{
					killerData->CurrentMultiKill = 1;
				}

				killerData->MultiKillResetTime = f.Time + f.GameConfig.MultiKillResetTime;

				f.Events.OnCombatEventPlayerKilledPlayer(playerDead, entityDead, playerKiller, entityKiller,
				                                         killerData->CurrentKillStreak, killerData->CurrentMultiKill);
			}
		}

		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			var dataPointer = gameContainer->PlayersData.GetPointer(playerDead);

			dataPointer->CurrentKillStreak = 0;
			dataPointer->CurrentMultiKill = 0;
		}
	}
}