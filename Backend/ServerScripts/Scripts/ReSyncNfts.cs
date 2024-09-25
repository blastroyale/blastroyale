using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using PlayFab;
using PlayFab.AdminModels;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;


public class ReSyncNfts : PlayfabScript
{
	public override Environment GetEnvironment() => Environment.DEV;
	
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
		var equipmentData = state.DeserializeModel<EquipmentData>();
		equipmentData.LastUpdateTimestamp = 0;
		state.UpdateModel(equipmentData);
		await SetUserState(profile.PlayerId, state);
		Console.WriteLine($"Set last update time for player {profile.PlayerId}");
	}
}