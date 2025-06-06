using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the signal to process player's <seealso cref="PlayerMatchData"/> statistics
	/// </summary>
	public unsafe class MatchDataSystem : SystemSignalsOnly, ISignalPlayerDead, ISignalHealthChangedFromAttacker,
										  ISignalPlayerKilledPlayer, ISignalSpecialUsed, ISignalPlayerKilledByBeingAFK
	{
		/// <inheritdoc />
		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			var dataPointer = gameContainer->PlayersData.GetPointer(playerDead);

			dataPointer->DeathCount++;
			dataPointer->LastDeathPosition = f.Unsafe.GetPointer<Transform2D>(entityDead)->Position;

			if (dataPointer->FirstDeathTime == FP._0)
			{
				dataPointer->FirstDeathTime = f.Time;
			}

			dataPointer->CurrentKillStreak = 0;
			dataPointer->CurrentMultiKill = 0;
		}

		/// <inheritdoc />
		public void PlayerKilledPlayer(Frame f, PlayerRef playerDead, EntityRef entityDead, PlayerRef playerKiller,
									   EntityRef entityKiller)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();

			if (playerDead != playerKiller)
			{
				var killerData = gameContainer->PlayersData.GetPointer(playerKiller);

				killerData->PlayersKilledCount++;
				killerData->CurrentKillStreak++;

				if (f.Time > killerData->MultiKillResetTime)
				{
					killerData->CurrentMultiKill = 1;
				}
				else
				{
					killerData->CurrentMultiKill++;
				}

				killerData->MultiKillResetTime = f.Time + f.GameConfig.MultiKillResetTime;
			}
			else
			{
				gameContainer->PlayersData.GetPointer(playerDead)->SuicideCount++;
			}
		}

		/// <inheritdoc />
		public void HealthChangedFromAttacker(Frame f, EntityRef entity, EntityRef attacker, int previousHealth)
		{
			if (entity == attacker)
			{
				return;
			}

			var stats = f.Unsafe.GetPointer<Stats>(entity);
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();

			if (f.Unsafe.TryGetPointer<PlayerCharacter>(attacker, out var playerAttacker))
			{
				var data = gameContainer->PlayersData.GetPointer(playerAttacker->Player);
				
				if (stats->CurrentHealth < previousHealth)
				{
					data->DamageDone += (uint) (previousHealth - stats->CurrentHealth);
				}
				else if (f.Has<PlayerCharacter>(entity))
				{
					data->HealingDone += (uint) (stats->CurrentHealth - previousHealth);
				}
			}

			if (f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var playerHit))
			{
				var data = gameContainer->PlayersData.GetPointer(playerHit->Player);
				
				if (stats->CurrentHealth < previousHealth)
				{
					data->DamageReceived += (uint) (previousHealth - stats->CurrentHealth);
				}
				else if (attacker.IsValid && f.Has<PlayerCharacter>(entity))
				{
					data->HealingReceived += (uint) (stats->CurrentHealth - previousHealth);
				}
			}
		}

		/// <inheritdoc />
		public void SpecialUsed(Frame f, EntityRef entity, int specialIndex)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(entity);

			gameContainer->PlayersData.GetPointer(playerCharacter->Player)->SpecialsUsedCount++;
		}

		public void PlayerKilledByBeingAFK(Frame f, PlayerRef player)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
			var playerData = gameContainer->PlayersData.GetPointer(player);
			playerData->KilledByBeingAFK = true;
		}
	}
}