using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using PlayFab;
using PlayFab.ServerModels;
using FirstLight.Server.SDK.Modules;
using Scripts.Base;

/// <summary>
/// Read all players from playfab and generate a csv
/// </summary>
public class AllPlayerDataToCsv : PlayfabScript
{
	public override Environment GetEnvironment() => Environment.DEV;

	public override void Execute(ScriptParameters parameters)
	{
		var task = RunAsync();
		task.Wait();
	}

	public async Task RunAsync()
	{
		var csvData = new List<Dictionary<string, string>>();
		foreach (var player in await GetAllPlayers())
		{
			var playerData = await ProcessPlayerData(player);
			if (playerData != null)
			{
				csvData.Add(playerData);
			}
		}
		var path = Path.GetDirectoryName(typeof(AllPlayerDataToCsv).Assembly.Location) + "/export.csv";
		FileUtil.ExportToCsv(path, csvData);
		Console.WriteLine($"Saved {csvData.Count} players data to {path}");
	}

	private async Task<Dictionary<string, string>> ProcessPlayerData(PlayerProfile profile)
	{
		Console.WriteLine($"Processing player {profile.PlayerId}");
		var userDataResult = await PlayFabServerAPI.GetUserReadOnlyDataAsync(new GetUserDataRequest()
		{
			Keys = new List<string>() {typeof(PlayerData).FullName},
			PlayFabId = profile.PlayerId
		});
		HandleError((userDataResult.Error));
		var userDataJson = userDataResult.Result.Data.ToDictionary(
		                                                           entry => entry.Key,
		                                                           entry => entry.Value.Value);
		var analyticsData = new Dictionary<string, string>();
		analyticsData["id"] = profile.PlayerId;
		analyticsData["name"] = profile.DisplayName;
		analyticsData["lastLoginDays"] = (DateTime.UtcNow - profile.LastLogin.Value).TotalDays.ToString();
		analyticsData["created"] =  (DateTime.UtcNow - profile.Created.Value).TotalDays.ToString();
		analyticsData["trophies"] = "-1";
		try
		{
			var playerData = ModelSerializer.DeserializeFromData<PlayerData>(userDataJson);
			analyticsData["trophies"] = playerData.Trophies.ToString();
		}
		catch (Exception e)
		{
			Console.WriteLine($"Error deserializing player {profile.PlayerId} - {e.Message}");
		}
		return analyticsData;
	}
}