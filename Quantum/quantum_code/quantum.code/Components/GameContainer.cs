using System;
using System.Collections.Generic;

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
		public QuantumPlayerMatchData[] GetPlayersMatchData(Frame f, out PlayerRef leader)
		{
			var data = PlayersData;
			var matchData = new List<QuantumPlayerMatchData>();
			var gameMode = f.RuntimeConfig.GameMode;
			
			leader = PlayerRef.None;

			// Process scores
			for (var i = 0; i < f.PlayerCount; i++)
			{
				var playerMatchData = new QuantumPlayerMatchData(f, data[i]);

				if (gameMode == GameMode.Deathmatch)
				{
					ProcessDMPlayerScore(ref playerMatchData);
				}
				else if (gameMode == GameMode.BattleRoyale)
				{
					ProcessBRPlayerScore(ref playerMatchData);
				}
				
				matchData.Add(playerMatchData);
			}
			
			matchData.Sort((playerMatchData1, playerMatchData2) => playerMatchData2.PlayerScore.CompareTo(playerMatchData1.PlayerScore));

			// Process ranks
			var currentScore = matchData[0].PlayerScore;
			var currentIndex = 0;
			for (var i = 0; i < f.PlayerCount; i++)
			{
				var playerMatchData = matchData[i];
				if (playerMatchData.PlayerScore != currentScore)
				{
					currentIndex = i;
					currentScore = playerMatchData.PlayerScore;
				}
				
				playerMatchData.PlayerRank = (uint)currentIndex + 1;

				matchData[i] = playerMatchData;
			}

			leader = matchData[0].Data.Player;
			
			return matchData.ToArray();
		}
		
		private void ProcessDMPlayerScore(ref QuantumPlayerMatchData playerMatchData)
		{
			playerMatchData.PlayerScore = 0;
			
			playerMatchData.PlayerScore += playerMatchData.Data.PlayersKilledCount * 10000;
			playerMatchData.PlayerScore -= playerMatchData.Data.DeathCount * 1000;
		}

		private void ProcessBRPlayerScore(ref QuantumPlayerMatchData playerMatchData)
		{
			playerMatchData.PlayerScore = 0;
			if (playerMatchData.Data.DeathCount == 0)
			{
				// Add 10000 for being alive
				playerMatchData.PlayerScore += 10000;
				// Add 1000 for each player we killed
				playerMatchData.PlayerScore += playerMatchData.Data.PlayersKilledCount * 1000;
			}
			else
			{
				// For the ones not alive we add the Death time. Match time should be around 300 seconds max
				// We multiply by 10 to give it more precision which gets around 3000 value max
				// so we won't surpass the 10000 for being alive for sure.
				playerMatchData.PlayerScore += Convert.ToUInt32(playerMatchData.Data.FirstDeathTime.AsFloat*10);
			}
		}
	}
}
