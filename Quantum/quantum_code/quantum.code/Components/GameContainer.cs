using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct GameContainer
	{
		/// <summary>
		/// Requests the information if this game has already been completed with success
		/// </summary>
		public bool IsGameCompleted => CurrentProgress >= TargetProgress;

		/// <summary>
		/// Add a PlayerMatchData to the container linked to a specific PlayerRef.
		/// </summary>
		internal void AddPlayer(Frame f, PlayerRef player, EntityRef playerEntity, uint playerLevel, GameId skin,
		                        GameId deathMarker, uint playerTrophies)
		{
			var isBot = f.TryGet<BotCharacter>(playerEntity, out var bot);
			
			PlayersData[player] = new PlayerMatchData
			{
				Entity = playerEntity,
				Player = player,
				PlayerLevel = playerLevel,
				PlayerSkin = skin,
				PlayerTrophies = playerTrophies,
				PlayerDeathMarker = isBot ? bot.DeathMarker : deathMarker,
				BotNameIndex = isBot ? bot.BotNameIndex : 0
			};
		}

		/// <summary>
		/// Remove an existing PlayerMatchData from the container that is linked to a specific PlayerRef.
		/// </summary>
		internal void RemovePlayer(Frame f, PlayerRef player)
		{
			if (IsGameCompleted)
			{
				return;
			}

			PlayersData[player] = new PlayerMatchData();
		}

		/// <summary>
		/// Updates the current game progress value by the given <paramref name="amount"/> of steps.
		/// It will mark the game as completed if the target value is reached in this update.
		/// </summary>
		internal void UpdateGameProgress(Frame f, uint amount)
		{
			if (amount <= 0)
			{
				return;
			}

			var previousProgress = CurrentProgress;

			CurrentProgress += amount;

			f.Events.OnGameProgressUpdated(previousProgress, CurrentProgress, TargetProgress);

			if (CurrentProgress >= TargetProgress)
			{
				f.Signals.GameEnded();
			}
		}
		
		/// <summary>
		/// Tests the game completion strategy where everyone should be dead but a certain amount of people defined in the TargetProgress
		/// </summary>
		internal void TestEveryoneIsDead(Frame f)
		{
			var playersAlive = f.ComponentCount<AlivePlayerCharacter>();

			CurrentProgress = (uint)( f.PlayerCount - playersAlive);

			if (CurrentProgress >= TargetProgress)
			{
				f.Signals.GameEnded();
			}
		}

		/// <summary>
		/// Request all players match data.
		/// Battle Royale Ranking: More frags == higher rank and Dead longer == lower rank
		/// Deathmatch Ranking: More frags == higher rank and Same frags && more deaths == lower rank 
		/// </summary>
		public List<QuantumPlayerMatchData> GetPlayersMatchData(Frame f, out PlayerRef leader)
		{
			var data = PlayersData;
			var playersData = new List<QuantumPlayerMatchData>(data.Length);
			var gameModeConfig = f.Context.GameModeConfig;
			var sorter = GetSorter(gameModeConfig.RankSorter);
			var rankProcessor = GetProcessor(gameModeConfig.RankProcessor);

			for (var i = 0; i < f.PlayerCount; i++)
			{
				playersData.InsertIntoSortedList(new QuantumPlayerMatchData(f, data[i]), sorter);
			}

			leader = playersData[0].Data.Player;

			for (var i = 0; i < playersData.Count; i++)
			{
				var player = playersData[i];

				player.PlayerRank = rankProcessor.ProcessRank(playersData, i, sorter);

				playersData[i] = player;
			}

			playersData.SortByPlayerRef(false);

			return playersData;
		}

		/// <summary>
		/// Generates a weapon <see cref="Equipment"/> from the equipment pool
		/// </summary>
		public Equipment GenerateNextWeapon(Frame f)
		{
			return DropPool.WeaponPool[f.RNG->Next(0, DropPool.WeaponPool.Length)];
		}

#region Player Rank Sorters
		
		private static IRankSorter GetSorter(RankSorter sorter)
		{
			return sorter switch
			{
				RankSorter.BattleRoyale => new BattleRoyaleSorter(),
				RankSorter.Deathmatch => new DeathmatchSorter(),
				_ => throw new ArgumentOutOfRangeException(nameof(sorter), sorter, null)
			};
		}

		private static IRankProcessor GetProcessor(RankProcessor processor)
		{
			return processor switch
			{
				RankProcessor.General => new GeneralRankProcessor(),
				RankProcessor.Deathmatch => new DeathMatchRankProcessor(),
				_ => throw new ArgumentOutOfRangeException(nameof(processor), processor, null)
			};
		}

		internal interface IRankSorter : IComparer<QuantumPlayerMatchData>
		{
		}

		internal interface IRankProcessor
		{
			public uint ProcessRank(IReadOnlyList<QuantumPlayerMatchData> playersData, int i, IRankSorter sorter);
		}

		private class DeathMatchRankProcessor : IRankProcessor
		{
			public uint ProcessRank(IReadOnlyList<QuantumPlayerMatchData> playersData, int i, IRankSorter sorter)
			{
				var rank = (uint) i + 1;

				if (i > 0 && sorter.Compare(playersData[i], playersData[i - 1]) == 0)
				{
					rank = playersData[i - 1].PlayerRank;
				}

				return rank;
			}
		}

		private class GeneralRankProcessor : IRankProcessor
		{
			public uint ProcessRank(IReadOnlyList<QuantumPlayerMatchData> playersData, int i, IRankSorter sorter)
			{
				return (uint) i + 1;
			}
		}

		private class BattleRoyaleSorter : IRankSorter
		{
			/// <inheritdoc />
			public int Compare(QuantumPlayerMatchData x, QuantumPlayerMatchData y)
			{
				var compare = x.Data.DeathCount.CompareTo(y.Data.DeathCount);

				if (compare == 0)
				{
					compare = x.Data.FirstDeathTime.CompareTo(y.Data.FirstDeathTime) * -1;
				}

				if (compare == 0)
				{
					compare = x.Data.PlayersKilledCount.CompareTo(y.Data.PlayersKilledCount) * -1;
				}

				if (compare == 0)
				{
					compare = x.Data.Player._index.CompareTo(y.Data.Player._index) * -1;
				}

				return compare;
			}
		}

		private class DeathmatchSorter : IRankSorter
		{
			/// <inheritdoc />
			public int Compare(QuantumPlayerMatchData x, QuantumPlayerMatchData y)
			{
				var compare = x.Data.PlayersKilledCount.CompareTo(y.Data.PlayersKilledCount) * -1;

				if (compare == 0)
				{
					compare = x.Data.DeathCount.CompareTo(y.Data.DeathCount);
				}

				return compare;
			}
		}

#endregion
	}
}