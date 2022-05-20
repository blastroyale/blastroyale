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
		class ReverseComparer : IComparer<string> {
			public int Compare(string x, string y) {
				return y.CompareTo(x);
			}
		}
		
		
		/// <summary>
		/// Request all players match data.
		/// Battle Royale Ranking: More frags == higher rank and Dead longer == lower rank
		/// Deathmatch Ranking: More frags == higher rank and Same frags && more deaths == lower rank 
		/// </summary>
		public QuantumPlayerMatchData[] GetPlayersMatchData(Frame f, out PlayerRef leader)
		{
			var data = PlayersData;
			var playersData = new List<QuantumPlayerMatchData>();
			var gameMode = f.RuntimeConfig.GameMode;
			
			leader = PlayerRef.None;

			if (gameMode == GameMode.Deathmatch)
			{
				playersData.Sort(DMComparer);
			}
			else
			{
				playersData.Sort(BRComparer);
			}

			for (var i = 0; i < f.PlayerCount; i++)
			{
				var player = new QuantumPlayerMatchData(f, data[i])
				{
					PlayerRank = (uint) i + 1
				};

				if (gameMode == GameMode.Deathmatch && i > 0 && 
				    DMComparer(player, playersData[i - 1]) == 0)
				{
					player.PlayerRank = playersData[i - 1].PlayerRank;
				}

				playersData.Add(player);
			}

			return playersData.ToArray();
		}
		
		private static int BRComparer(QuantumPlayerMatchData x, QuantumPlayerMatchData y)
		{
			var compare = x.Data.DeathCount.CompareTo(y.Data.DeathCount);

			if (compare == 0)
			{
				compare = x.Data.FirstDeathTime.CompareTo(y.Data.FirstDeathTime);
			}

			if (compare == 0)
			{
				compare = x.Data.PlayersKilledCount.CompareTo(y.Data.PlayersKilledCount);
			}

			if (compare == 0)
			{
				compare = x.Data.Player._index.CompareTo(y.Data.Player._index);
			}

			return compare;
		}

		private static int DMComparer(QuantumPlayerMatchData x, QuantumPlayerMatchData y)
		{
			var compare = x.Data.PlayersKilledCount.CompareTo(y.Data.PlayersKilledCount);

			if (compare == 0)
			{
				compare = x.Data.DeathCount.CompareTo(y.Data.DeathCount);
			}

			return compare;
		}
	}
}
