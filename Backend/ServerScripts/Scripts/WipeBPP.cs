using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using PlayFab;
using PlayFab.AdminModels;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

/// <summary>
/// Script to wipe BPP
/// </summary>
public class WipeBPP : PlayfabScript
{
	public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.DEV;
	private const uint SEASON_TO_WIPE = 6;

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

	private async Task Proccess(PlayerProfile profile)
	{
		var state = await ReadUserState(profile.PlayerId);

		if (state != null)
		{
			var playerData = state.DeserializeModel<BattlePassData>();
			if (playerData.Seasons.TryGetValue(SEASON_TO_WIPE, out var season))
			{
				season.Level = 0;
				season.Points = 0;
			}

			state.UpdateModel(playerData);
			await SetUserState(profile.PlayerId, state);
			Console.WriteLine($"Wiped BPP for player {profile.PlayerId}");
		}
	}
}