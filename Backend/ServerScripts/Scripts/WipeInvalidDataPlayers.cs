using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.ServerModels;
using FirstLight.Server.SDK.Modules;

/// <summary>
/// Wipes all player data if the player contains invalid data.
/// The player data should be renewed the next time he logs in.
/// </summary>
public class WipeInvalidDataPlayers : PlayfabScript
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
		foreach (var player in await GetAllPlayers())
		{
			tasks.Add(CheckPlayerData(player));
		}
		Task.WaitAll(tasks.ToArray());
		Console.WriteLine("Done !");
	}

	private async Task CheckPlayerData(PlayerProfile profile)
	{
		Console.WriteLine($"Processing player {profile.PlayerId}");
		var userDataResult = await PlayFabServerAPI.GetUserReadOnlyDataAsync(new GetUserDataRequest()
		{
			PlayFabId = profile.PlayerId
		});
		HandleError((userDataResult.Error));
		var userDataJson = userDataResult.Result.Data.ToDictionary(
		                                                           entry => entry.Key,
		                                                           entry => entry.Value.Value);
		try
		{
			// Deprecated field from 0.3 to 0.4
			if (userDataJson.ContainsKey("FirstLight.Game.Data.NftEquipmentData"))
				throw new Exception("Deprecated");
			
			ModelSerializer.DeserializeFromData<PlayerData>(userDataJson);
			ModelSerializer.DeserializeFromData<EquipmentData>(userDataJson);
			ModelSerializer.DeserializeFromData<RngData>(userDataJson);
			ModelSerializer.DeserializeFromData<IdData>(userDataJson);
			Console.WriteLine($"{profile.PlayerId} had valid data");
		}
		catch (Exception e)
		{
			Console.WriteLine($"{profile.PlayerId} had invalid data, wiping");
			await PlayFabServerAPI.UpdateUserReadOnlyDataAsync(new UpdateUserDataRequest()
			{
				PlayFabId = profile.PlayerId,
				KeysToRemove = new List<string>()
				{
					"NftEquipmentData",
					typeof(PlayerData).FullName,
					typeof(EquipmentData).FullName,
					typeof(RngData).FullName,
					typeof(IdData).FullName,
				}
			});
			Console.WriteLine($"Wiped data for player {profile.PlayerId}");
		}
	}
}