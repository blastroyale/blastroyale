using System;
using System.Collections.Generic;
using System.Linq;

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
		internal void AddPlayer(Frame f, PlayerRef player, EntityRef playerEntity, uint playerLevel, GameId skin, uint playerTrophies)
		{
			PlayersData[player] = new PlayerMatchData
			{
				Entity = playerEntity,
				Player = player,
				PlayerLevel = playerLevel,
				PlayerSkin = skin,
				PlayerTrophies = playerTrophies,
				BotNameIndex = f.TryGet<BotCharacter>(playerEntity, out var bot) ? bot.BotNameIndex : 0
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
		/// Request all players match data.
		/// Battle Royale Ranking: More frags == higher rank and Dead longer == lower rank
		/// Deathmatch Ranking: More frags == higher rank and Same frags && more deaths == lower rank 
		/// </summary>
		public List<QuantumPlayerMatchData> GetPlayersMatchData(Frame f, out PlayerRef leader)
		{
			var data = PlayersData;
			var playersData = new List<QuantumPlayerMatchData>(data.Length);
			var gameMode = f.Context.MapConfig.GameMode;
			IRankSorter sorter;

			if (gameMode == GameMode.Deathmatch)
			{
				sorter = new DeathmatchSorter();
			}
			else
			{
				sorter = new BattleRoyaleSorter();
			}
			
			for (var i = 0; i < f.PlayerCount; i++)
			{
				playersData.InsertIntoSortedList(new QuantumPlayerMatchData(f, data[i]), sorter);
			}

			leader = playersData[0].Data.Player;

			for (var i = 0; i < playersData.Count; i++)
			{
				var player = playersData[i];

				player.PlayerRank = RankProcessor(playersData, i, sorter);

				playersData[i] = player;
			}

			playersData.SortByPlayerRef(false);

			return playersData;
		}
		
		private uint RankProcessor(IReadOnlyList<QuantumPlayerMatchData> playersData, int i, IRankSorter sorter)
		{
			var rank = (uint) i + 1;

			if (sorter.GameMode == GameMode.Deathmatch && i > 0 &&
			    sorter.Compare(playersData[i], playersData[i - 1]) == 0)
			{
				rank = playersData[i - 1].PlayerRank;
			}

			return rank;
		}

#region Player Rank Sorters
		private interface IRankSorter : IComparer<QuantumPlayerMatchData>
		{
			/// <summary>
			/// Requests the <see cref="GameMode"/> defined for this sorter
			/// </summary>
			public GameMode GameMode { get; }
		}
		
		private class BattleRoyaleSorter : IRankSorter
		{
			/// <inheritdoc />
			public GameMode GameMode => GameMode.BattleRoyale;
			
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
			public GameMode GameMode => GameMode.Deathmatch;
			
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
