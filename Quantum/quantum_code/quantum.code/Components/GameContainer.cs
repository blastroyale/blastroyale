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
		internal void AddPlayer(Frame f, PlayerRef player, EntityRef playerEntity, uint playerLevel, GameId skin)
		{
			PlayersData[player] = new PlayerMatchData
			{
				Entity = playerEntity,
				Player = player,
				PlayerLevel = playerLevel,
				PlayerSkin = skin,
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
		/// Battle Roayle Ranking: More frags == higher rank and Dead longer == lower rank
		/// Deathmatch Ranking: More frags == higher rank and Same frags && more deaths == lower rank 
		/// </summary>
		public QuantumPlayerMatchData[] GetPlayersMatchData(Frame f, out PlayerRef leader)
		{
			var data = PlayersData;
			var matchData = new QuantumPlayerMatchData[f.RuntimeConfig.PlayersLimit];
			var gameMode = f.RuntimeConfig.GameMode;

			leader = PlayerRef.None;

			for (var i = 0; i < f.RuntimeConfig.PlayersLimit; i++)
			{
				matchData[i] = new QuantumPlayerMatchData(f, data[i]);

				if (gameMode == GameMode.Deathmatch)
				{
					ProcessDeathmatchRanks(matchData, i);
				}
				else if (gameMode == GameMode.BattleRoyale)
				{
					ProcessBattleRoyaleRanks(matchData, i);
				}

				if (matchData[i].PlayerRank == 1)
				{
					leader = matchData[i].Data.Player;
				}
			}

			return matchData;
		}

		private void ProcessDeathmatchRanks(QuantumPlayerMatchData[] data, int i)
		{
			var pos = 1u;
			
			for (var j = 0; j < i; j++)
			{
				if (data[i].Data.PlayersKilledCount < data[j].Data.PlayersKilledCount)
				{
					pos++;
				}
				else if (data[i].Data.PlayersKilledCount > data[j].Data.PlayersKilledCount || 
				         data[i].Data.DeathCount < data[j].Data.DeathCount)
				{
					data[j].PlayerRank++;
				}
			}
			
			data[i].PlayerRank = pos;
		}

		private void ProcessBattleRoyaleRanks(QuantumPlayerMatchData[] data, int i)
		{
			var pos = 1u;
			
			for (var j = 0; j < i; j++)
			{
				if (data[i].Data.FirstDeathTime < data[j].Data.FirstDeathTime)
				{
					pos++;
				}
				else if (data[i].Data.FirstDeathTime > data[j].Data.FirstDeathTime || 
				         data[i].Data.PlayersKilledCount > data[j].Data.PlayersKilledCount)
				{
					data[j].PlayerRank++;
				}
			}
			
			data[i].PlayerRank = pos;
		}
	}
}