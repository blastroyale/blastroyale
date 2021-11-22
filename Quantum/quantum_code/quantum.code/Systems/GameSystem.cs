namespace Quantum.Systems
{
	/// <summary>
	/// This system handles the behaviour when the game systems, the ending and is the final countdown to quit the screen
	/// </summary>
	public unsafe class GameSystem : SystemMainThread, 
	                                 ISignalGameEnded, ISignalPlayerKilledPlayer
	{
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			f.ResolveList(f.Global->Queries).Clear();
		}

		/// <inheritdoc />
		public void GameEnded(Frame f)
		{
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			var matchData = container->PlayersData;
			var playerWinner = (PlayerRef) 0;

			for (var i = 1; i < f.RuntimeConfig.TotalFightersLimit; i++)
			{
				if (matchData[i].PlayersKilledCount > matchData[playerWinner].PlayersKilledCount)
				{
					playerWinner = i;
				}
			}
			
			foreach (var projectile in f.GetComponentIterator<Projectile>())
			{
				f.Destroy(projectile.Entity);
			}
			
			foreach (var hazard in f.GetComponentIterator<Hazard>())
			{
				f.Destroy(hazard.Entity);
			}

			f.Events.OnGameEnded(playerWinner, matchData[playerWinner]);
			
			f.SystemDisable(typeof(AiPreUpdateSystem));
			f.SystemDisable(typeof(AiSystem));
			f.SystemDisable(typeof(Core.NavigationSystem));
			f.SystemDisable(typeof(BotCharacterSystem));
			f.SystemDisable(typeof(PlayerCharacterSystem));
			f.SystemDisable(typeof(ProjectileSystem));
			f.SystemDisable(typeof(HazardSystem));
		}

		/// <inheritdoc />
		public void PlayerKilledPlayer(Frame f, PlayerRef playerDead, EntityRef entityDead, PlayerRef playerKiller,
		                               EntityRef entityKiller)
		{
			if (playerDead == playerKiller)
			{
				return;
			}

			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			var killerData = container->PlayersData[playerKiller];
			var killDiff = killerData.PlayersKilledCount - container->CurrentProgress;

			if (killDiff > 0)
			{
				container->UpdateGameProgress(f, killDiff);
			}
		}
	}
}