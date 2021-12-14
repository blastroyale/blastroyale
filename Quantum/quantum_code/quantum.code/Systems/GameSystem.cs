namespace Quantum.Systems
{
	/// <summary>
	/// This system handles the behaviour when the game systems, the ending and is the final countdown to quit the screen
	/// </summary>
	public unsafe class GameSystem : SystemMainThread,
	                                 ISignalGameEnded, ISignalHealthIsZero
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

			for (var i = 1; i < f.RuntimeConfig.PlayersLimit; i++)
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
		public void HealthIsZero(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (entity == attacker || !f.TryGet<PlayerCharacter>(entity, out var player))
			{
				return;
			}
			
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			var inc = 0u;

			if (container->GameMode == GameMode.BattleRoyale)
			{
				inc = 1;
			}
			else if(container->GameMode == GameMode.Deathmatch &&
			        f.TryGet<PlayerCharacter>(attacker, out var killer))
			{
				var killerData = container->PlayersData[killer.Player];
				
				inc = killerData.PlayersKilledCount - container->CurrentProgress;
			}
			
			container->UpdateGameProgress(f, inc);
		}
	}
}