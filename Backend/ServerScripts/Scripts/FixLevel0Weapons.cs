using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

/// <summary>
/// Script to fix level 0 weapons by replacing it with level 1
/// </summary>
public class FixLevel0Weapons : PlayfabScript
{
	
	private static int BAD = 0;
	
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
		Console.WriteLine("Done ! Fixed "+BAD+" weapons level 0");
	}

	private async Task Proccess(PlayerProfile profile)
	{
		var state = await ReadUserState(profile.PlayerId);
		if (state == null || !state.Has<EquipmentData>())
		{
			return;
		}
		var equipmentData = state.DeserializeModel<EquipmentData>();
		bool bad = false;
		foreach (var id in equipmentData.Inventory.Keys)
		{
			var item = equipmentData.Inventory[id];
			if (item.Level == 0)
			{
				BAD++;
				Console.WriteLine("Item level 0");
				item.Level = 1;
				equipmentData.Inventory[id] = item;
				bad = true;
			}
		}

		if (bad)
		{
			state.UpdateModel(equipmentData);
			await SetUserState(profile.PlayerId, state);
			Console.WriteLine($"Fixed items for player {profile.PlayerId}");
		}
	
	}
}