using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using PlayFab;
using PlayFab.AdminModels;
using PlayFab.ServerModels;
using Quantum;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

/// <summary>
/// Script to wipe leaderboards and give rewards
/// </summary>
public class WipeLeaderboards : PlayfabScript
{
	private List<string> wiped = new List<string>();
	private List<string> error = new List<string>();
	
	public override Environment GetEnvironment() => Environment.PROD;
	public override void Execute(ScriptParameters parameters)
    {
    	var task = RunAsync();
    	task.Wait();
	}
	
	public async Task RunAsync()
	{
		var tasks = new List<Task>();
		var batchSize = 10000;
		var allPlayers = await GetAllPlayers();
		foreach (var player in allPlayers)
		{
			tasks.Add(Proccess(player));
			if (tasks.Count >= batchSize)
			{
				Console.WriteLine("Waiting Batch Proccess");
				await Task.WhenAll(tasks.ToArray());
				tasks.Clear();
			}
		}
		Console.WriteLine("Done !");
		Console.WriteLine("Wipes: "+wiped.Count);
		Console.WriteLine("Errors: "+string.Join(",", error));
	}

	private async Task Proccess(PlayerProfile profile)
	{
		var state = await ReadUserState(profile);
		if (state == null || !state.Has<PlayerData>())
		{
			error.Add(profile.PlayerId);
			return;
		}
		//GiveRewards(profile, state);
		var playerData = state.DeserializeModel<PlayerData>();
		playerData.Trophies = 0;
		state.UpdateModel(playerData);
		await SetUserState(profile.PlayerId, state);
		//Console.WriteLine($"Wiped Trophies for player {profile.PlayerId}");
		wiped.Add(profile.PlayerId);
	}
}