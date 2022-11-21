using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles the behaviour when the game systems, the ending and is the final countdown to quit the screen
	/// </summary>
	public unsafe class GameSystem : SystemMainThread, ISignalOnComponentAdded<GameContainer>,
	                                 ISignalGameEnded, ISignalPlayerDead, ISignalPlayerKilledPlayer, ISignalOnPlayerDataSet, ISignalAllPlayersJoined
	{

		/// <summary>
		/// Time while the simulation will wait for players to connect to start the game.
		/// This has to take in account server web requests to validate user data
		/// </summary>
		private static FP PLAYERS_JOIN_TIMEOUT = 60;
		
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			f.ResolveList(f.Global->Queries).Clear();
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			
			if (!container->IsGameStarted && f.Time > PLAYERS_JOIN_TIMEOUT)
			{
				f.Signals.AllPlayersJoined();
				container->IsGameStarted = true;
			}
		}

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, GameContainer* component)
		{
			switch (f.Context.GameModeConfig.CompletionStrategy)
			{
				case GameCompletionStrategy.Never:
					break;
				case GameCompletionStrategy.EveryoneDead:
					component->TargetProgress = (uint) f.PlayerCount - 1;
					break;
				case GameCompletionStrategy.KillCount:
					component->TargetProgress = f.Context.GameModeConfig.CompletionKillCount;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			SetupWeaponPool(f, component);
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
		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			if (f.Context.GameModeConfig.CompletionStrategy == GameCompletionStrategy.EveryoneDead)
			{
				var container = f.Unsafe.GetPointerSingleton<GameContainer>();
				container->TestEveryoneIsDead(f);
			}
		}

		/// <inheritdoc />
		public void PlayerKilledPlayer(Frame f, PlayerRef playerDead, EntityRef entityDead, PlayerRef playerKiller,
		                               EntityRef entityKiller)
		{
			if (f.Context.GameModeConfig.CompletionStrategy == GameCompletionStrategy.KillCount)
			{
				var container = f.Unsafe.GetPointerSingleton<GameContainer>();
				var inc = container->PlayersData[playerKiller].PlayersKilledCount - container->CurrentProgress;

				container->UpdateGameProgress(f, inc);
			}
		}

		private void SetupWeaponPool(Frame f, GameContainer* component)
		{
			var isHammerTimeMutator = f.Context.TryGetMutatorByType(MutatorType.HammerTime, out _);
			var offPool = new List<GameId>(GameIdGroup.Weapon.GetIds());
			var count = isHammerTimeMutator ? 0 : component->DropPool.WeaponPool.Length;
			var rarity = 0;

			offPool.Remove(GameId.Hammer);

			for (var i = 0; i < count; i++)
			{
				var playerData = f.GetPlayerData(i);
				var equipment = playerData?.Weapon;

				if (!equipment.HasValue || !equipment.Value.IsValid() || equipment.Value.IsDefaultItem())
				{
					var index = f.RNG->Next(0, offPool.Count);
					
					equipment = new Equipment(offPool[index]);
					
					if (offPool.Count > 1)
					{
						offPool.RemoveAt(index);
					}
				}

				rarity += (int) equipment.Value.Rarity;
				component->DropPool.WeaponPool[i] = equipment.Value;
			}

			component->DropPool.AverageRarity = (EquipmentRarity) FPMath.FloorToInt((FP) rarity / (count + 1));
			component->DropPool.MedianRarity = component->DropPool.WeaponPool[count / 2].Rarity;
		}
		
		public void OnPlayerDataSet(Frame f, PlayerRef player)
		{
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();

			if (container->IsGameStarted) return;
			
			if (HaveAllPlayersJoined(f))
			{
				f.Signals.AllPlayersJoined();
				container->IsGameStarted = true;
			}
		}

		private bool HaveAllPlayersJoined(Frame f)
		{
			var setupPlayers = 0;
			var expectedPlayers = 0;
			
			for (var x = 0; x < f.PlayerCount; x++)
			{
				if ((f.GetPlayerInputFlags(x) & DeterministicInputFlags.PlayerNotPresent) == 0)
				{
					expectedPlayers++;
				}

				if (f.GetPlayerData(x) != null)
				{
					setupPlayers++;
				}
			}
			return setupPlayers == expectedPlayers;
		}

		public void AllPlayersJoined(Frame f)
		{
			f.Events.OnAllPlayersJoined();
		}
	}
}