using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.ServerModels;
using Quantum;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

/// <summary>
/// Script sample for event rewards
/// </summary>
public class EventRewards : PlayfabScript
{
	public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.DEV;
	
	public override void Execute(ScriptParameters parameters)
	{
		var task = RunAsync();
		task.Wait();
	}

	public async Task RunAsync()
	{
		var tasks = new List<Task>();
		var leaderboard = await GetLeaderboard(0, 100, GameConstants.Network.LEADERBOARD_LADDER_NAME);
		foreach (var player in leaderboard)
		{
			tasks.Add(Proccess(player));
		}
		Task.WaitAll(tasks.ToArray());
		Console.WriteLine("Done !");
	}

	private void AddReward(PlayerData data, GameId id, int amt)
	{
		data.UncollectedRewards.Add(new RewardData()
		{
			Value = amt, RewardId = id
		});
	}

	private async Task Proccess(PlayerLeaderboardEntry entry)
	{
		var state = await ReadUserState(entry.PlayFabId);
		if (state == null || !state.Has<PlayerData>())
		{
			return;
		}
		var playerData = state.DeserializeModel<PlayerData>();
		var rank = entry.Position + 1;
		if (rank == 1)
		{
			AddReward(playerData, GameId.COIN, 20000);
			AddReward(playerData, GameId.CS, 10000);
		} else if (rank == 2)
		{
			AddReward(playerData, GameId.COIN, 14000);
			AddReward(playerData, GameId.CS, 7000);
		} else if (rank == 3)
		{
			AddReward(playerData, GameId.COIN, 10000);
			AddReward(playerData, GameId.CS, 5000);
		} else if (rank >= 4 && entry.Position <= 10)
		{
			AddReward(playerData, GameId.COIN, 8000);
			AddReward(playerData, GameId.CS, 4000);
		} else if (rank >= 11 && entry.Position <= 20)
		{
			AddReward(playerData, GameId.COIN, 6000);
			AddReward(playerData, GameId.CS, 3000);
		} else if (rank >= 21 && entry.Position <= 50)
		{
			AddReward(playerData, GameId.COIN, 4000);
			AddReward(playerData, GameId.CS, 2000);
		}else if (rank >= 51)
		{
			AddReward(playerData, GameId.COIN, 2000);
			AddReward(playerData, GameId.CS, 1000);
		}
		state.UpdateModel(playerData);
		await SetUserState(entry.PlayFabId, state);
		Console.WriteLine($"Player {entry.PlayFabId} Rank {rank} Trophies {playerData.Trophies} ");
	}
}