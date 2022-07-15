using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles the behaviour when the game systems, the ending and is the final countdown to quit the screen
	/// </summary>
	public unsafe class GameSystem : SystemMainThread, ISignalOnComponentAdded<GameContainer>,
	                                 ISignalGameEnded, ISignalHealthIsZero
	{
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			f.ResolveList(f.Global->Queries).Clear();
		}

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, GameContainer* component)
		{
			component->TargetProgress = f.Context.MapConfig.GameEndTarget;
		}

		/// <inheritdoc />
		public void GameEnded(Frame f)
		{
			var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();

			gameContainer->GameOverTime = f.Time;
			gameContainer->IsGameOver = true;
			
			f.Events.OnGameEnded();
			
			f.SystemDisable(typeof(AiPreUpdateSystem));
			f.SystemDisable(typeof(AiSystem));
			f.SystemDisable(typeof(Core.NavigationSystem));
			f.SystemDisable(typeof(BotCharacterSystem));
			f.SystemDisable(typeof(PlayerCharacterSystem));
			f.SystemDisable(typeof(ProjectileSystem));
			f.SystemDisable(typeof(HazardSystem));
			f.SystemDisable(typeof(SpellSystem));
			f.SystemDisable(typeof(ShrinkingCircleSystem));
		}

		/// <inheritdoc />
		public void HealthIsZero(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (!f.Has<PlayerCharacter>(entity))
			{
				return;
			}
			
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			var inc = 0u;

			if (f.Context.MapConfig.GameMode == GameMode.BattleRoyale)
			{
				inc = 1;
			}
			else if(entity != attacker && f.TryGet<PlayerCharacter>(attacker, out var killer))
			{
				var killerData = container->PlayersData[killer.Player];
				
				inc = killerData.PlayersKilledCount - container->CurrentProgress;
			}
			
			container->UpdateGameProgress(f, inc);
		}
	}
}