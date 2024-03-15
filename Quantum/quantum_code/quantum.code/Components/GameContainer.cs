using System;
using System.Collections.Generic;
using System.Linq;
using Quantum.Collections;

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
		internal void AddPlayer(Frame f, PlayerCharacterSetup setup)
		{
			var isBot = f.TryGet<BotCharacter>(setup.e, out var bot);
			var data = PlayersData[setup.playerRef];
			data.Entity = setup.e;
			data.Player = setup.playerRef;
			data.PlayerLevel = (ushort)setup.playerLevel;
			data.PlayerTrophies = setup.trophies;
			data.TeamId = setup.teamId;
			data.DeathFlag = setup.deathFlagID;
			data.BotNameIndex = isBot ? (short)bot.BotNameIndex : (short)0;
			var skins = f.ResolveList(data.Cosmetics);
			if (f.TryGet<CosmeticsHolder>(setup.e, out var cosmeticsHolder))
			{
				var skinListFromComponent = f.ResolveList(cosmeticsHolder.Cosmetics);

				foreach (var skinId in skinListFromComponent)
				{
					skins.Add(skinId);
				}
			}

			PlayersData[setup.playerRef] = data;
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
			var teamsAlive = new HashSet<int>();
			foreach (var (entity, _) in f.Unsafe.GetComponentBlockIterator<AlivePlayerCharacter>())
			{
				if (f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var t))
				{
					teamsAlive.Add(t->TeamId);
				}
			}

			// We count how many teams are alive towards our goal (we remove ours)
			var teamsAliveForGoal = teamsAlive.Count - 1;
			CurrentProgress = (uint)(TargetProgress - teamsAliveForGoal);

			if (CurrentProgress >= TargetProgress)
			{
				f.Signals.GameEnded();
			}
		}

		/// <summary>
		/// Request all players match data.
		/// Battle Royale Ranking: More frags == higher rank and Dead longer == lower rank
		/// 
		/// Deathmatch Ranking: More frags == higher rank and Same frags && more deaths == lower rank 
		/// </summary>
		public List<QuantumPlayerMatchData> GeneratePlayersMatchData(Frame f, out PlayerRef leader, out int leaderTeam)
		{
			var data = PlayersData;
			var gameModeConfig = f.Context.GameModeConfig;
			var sorter = GetSorter(gameModeConfig.RankSorter);
			var rankProcessor = GetProcessor(gameModeConfig.RankProcessor);

			var playersMatchData = new List<PlayerMatchData>(data.Length);
			for (var i = 0; i < f.PlayerCount; i++)
			{
				playersMatchData.Add(data[i]);
			}

			// TODO: Could be improved, but since this method is not called often I think this is ok.
			var playersData = playersMatchData
				// Group players by TeamID, and convert them to QuantumPlayerMatchData
				.GroupBy(pmd => pmd.TeamId, pmd => new QuantumPlayerMatchData(f, pmd))
				// Sort players inside of the gruop
				.Select(g => g
					.OrderBy(qpmd => qpmd, sorter))
				// Sort groups based on the same sorter, but use the first player in the group as the "sorting key"
				.OrderBy(g => g.First(), sorter)
				// Flatten back to list
				.SelectMany(x => x)
				.ToList();

			leader = playersData[0].Data.Player;
			leaderTeam = playersData[0].TeamId;

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
				RankSorter.BattleRoyale       => new BattleRoyaleSorter(),
				RankSorter.BattleRoyaleSquads => new BattleRoyaleSquadsSorter(),
				RankSorter.Deathmatch         => new DeathmatchSorter(),
				_                             => throw new ArgumentOutOfRangeException(nameof(sorter), sorter, null)
			};
		}

		private static IRankProcessor GetProcessor(RankProcessor processor)
		{
			return processor switch
			{
				RankProcessor.General    => new GeneralRankProcessor(),
				RankProcessor.Squads     => new SquadsRankProcessor(),
				RankProcessor.Deathmatch => new DeathMatchRankProcessor(),
				_                        => throw new ArgumentOutOfRangeException(nameof(processor), processor, null)
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
				var rank = (uint)i + 1;

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
				// First player is always ranked 1
				if (i == 0)
				{
					return 1;
				}

				// If the player is in the same team as the one before, keep the same rank
				if (playersData[i - 1].TeamId == playersData[i].TeamId)
				{
					return playersData[i - 1].PlayerRank;
				}

				// Otherwise, increase the rank
				return playersData[i - 1].PlayerRank + 1;
			}
		}

		private class SquadsRankProcessor : GeneralRankProcessor
		{
			// TODO: Add proper logic for the squads rank processor
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


		private class BattleRoyaleSquadsSorter : BattleRoyaleSorter
		{
			// TODO: Add proper logic for squads ranking sort
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