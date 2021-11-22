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
			var previousProgress = CurrentProgress;
			
			CurrentProgress += amount;

			f.Events.OnGameProgressUpdated(previousProgress, CurrentProgress, TargetProgress);

			if (CurrentProgress < TargetProgress)
			{
				return;
			}

			f.Signals.GameEnded();
		}

		/// <summary>
		/// Updates the current game ranks based on the player's performance.
		/// More frags == higher rank
		/// Same frags && more deaths == lower rank 
		/// </summary>
		internal void UpdateRanks(Frame f)
		{
			var data = PlayersData;
			
			for (var i = 0; i < f.RuntimeConfig.TotalFightersLimit; i++)
			{
				var pos = 1u;

				for (var j = 0; j < i; j++)
				{
					if (data[i].PlayersKilledCount < data[j].PlayersKilledCount)
					{
						pos++;
					}
					else if (data[i].PlayersKilledCount > data[j].PlayersKilledCount || 
					         data[i].DeathCount < data[j].DeathCount)
					{
						data.GetPointer(j)->CurrentKillRank++;
					}
				}

				data.GetPointer(i)->CurrentKillRank = pos;
			}
		}
	}
}