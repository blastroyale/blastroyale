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
	public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.STAGING;
	public override void Execute(ScriptParameters parameters)
    {
    	var task = RunAsync();
    	task.Wait();
	}
	
	public async Task RunAsync()
	{
		var tasks = new List<Task>();
		foreach (var player in await GetAllPlayers())
		{
			tasks.Add(Proccess(player));
		}
		Task.WaitAll(tasks.ToArray());
		Console.WriteLine("Done !");
	}

	private void GiveRewards(PlayerProfile profile, ServerState state)
	{
		var playerData = state.DeserializeModel<PlayerData>();
		playerData.Currencies.TryGetValue(GameId.COIN, out var coins);
		coins += 1000;
		playerData.Currencies[GameId.COIN] = coins;
		if (playerData.Trophies >= 1200)
		{
			playerData.UncollectedRewards.Add(ItemFactory.Currency(GameId.CS, 500));
		}
		state.UpdateModel(playerData);
	}

	private async Task Proccess(PlayerProfile profile)
	{
		var state = await ReadUserState(profile.PlayerId);
		if (state == null || !state.Has<PlayerData>())
		{
			return;
		}
		GiveRewards(profile, state);
		var playerData = state.DeserializeModel<PlayerData>();
		playerData.Trophies = 1000;
		state.UpdateModel(playerData);
		await SetUserState(profile.PlayerId, state);
		Console.WriteLine($"Wiped Trophies for player {profile.PlayerId}");
	}
}