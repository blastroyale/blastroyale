using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum.Systems.Bots;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles the behaviour when the game systems, the ending and is the final countdown to quit the screen
	/// </summary>
	public unsafe class GameSystem : SystemMainThread, ISignalOnComponentAdded<GameContainer>,
									 ISignalGameEnded, ISignalPlayerDead, ISignalPlayerKilledPlayer,
									 ISignalOnPlayerDataSet
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
				AllPlayersJoined(f, container);
			}
		}

		public void OnAdded(Frame f, EntityRef entity, GameContainer* component)
		{
			switch (f.Context.GameModeConfig.CompletionStrategy)
			{
				case GameCompletionStrategy.Never:
					component->TargetProgress = uint.MaxValue;
					break;
				case GameCompletionStrategy.EveryoneDead:
					// Set after AllPlayersJoined
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

			foreach (var livingPlayer in f.GetComponentIterator<AlivePlayerCharacter>())
			{
				if (f.TryGet<PlayerCharacter>(livingPlayer.Entity, out var playerCharacter) && !f.Has<BotCharacter>(livingPlayer.Entity))
				{
					f.ServerCommand(playerCharacter.Player, QuantumServerCommand.EndOfGameRewards);
				}
			}

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
				// TODO mihak: Make squads work with KillCount mode.
				var inc = container->PlayersData[playerKiller].PlayersKilledCount - container->CurrentProgress;

				container->UpdateGameProgress(f, inc);
			}
		}

		private void SetupWeaponPool(Frame f, GameContainer* component)
		{
			var isHammerTimeMutator = f.Context.TryGetMutatorByType(MutatorType.HammerTime, out _);
			var offPool = new List<GameId>(GameIdGroup.Weapon.GetIds().Where(item => !item.IsInGroup(GameIdGroup.Deprecated)));
			var count = isHammerTimeMutator ? 0 : component->DropPool.WeaponPool.Length;

			offPool.Remove(GameId.Hammer);

			if (!isHammerTimeMutator && f.Context.TryGetWeaponLimiterMutator(out var forcedWeaponId))
			{
				for (var i = 0; i < count; i++)
				{
					component->DropPool.WeaponPool[i] = Equipment.Create(f, forcedWeaponId, EquipmentRarity.Common, 1);
				}
			}
		}

		public void OnPlayerDataSet(Frame f, PlayerRef player)
		{
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();

			if (!container->IsGameStarted && HaveAllPlayersJoined(f))
			{
				AllPlayersJoined(f, container);
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

		private void AllPlayersJoined(Frame f, GameContainer* container)
		{
			f.Signals.AllPlayersJoined();
			RefreshTotalTeamCount(f);

			f.Events.OnAllPlayersJoined();
			container->IsGameStarted = true;
		}

		private void RefreshTotalTeamCount(Frame f)
		{
			var teams = new HashSet<int>();

			foreach (var (_, pc) in f.Unsafe.GetComponentBlockIterator<PlayerCharacter>())
			{
				teams.Add(pc->TeamId);
			}

			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			
			// The target of the game is that all teams die but one (ourselves)
			container->TargetProgress = (uint) teams.Count -1;
		}
	}
}