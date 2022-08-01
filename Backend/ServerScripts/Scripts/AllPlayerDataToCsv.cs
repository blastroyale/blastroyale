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
using Scripts.Base;

namespace Scripts;

/// <summary>
/// Read all players from playfab and generate a csv
/// </summary>
public class AllPlayerDataToCsv : PlayfabScript
{
	public override string GetPlayfabTitle() => "***REMOVED***";
	public override string GetPlayfabSecret() => "***REMOVED***";

	public const string SEGMENT_ID = "97EC6C2DE051B678";

	public override void Execute(ScriptParameters parameters)
	{
		var task = RunAsync();
		task.Wait();
	}

	public async Task RunAsync()
	{
		var segmentResult = await PlayFabServerAPI.GetPlayersInSegmentAsync(new GetPlayersInSegmentRequest()
		{
			SegmentId = SEGMENT_ID, // All players
			MaxBatchSize = 10000
		});
		HandleError(segmentResult.Error);
		Console.WriteLine($"Processing {segmentResult.Result.PlayerProfiles.Count} Players");
		var csvData = new List<Dictionary<string, string>>();
		foreach (var player in segmentResult.Result.PlayerProfiles)
		{
			var playerData = await ProcessPlayerData(player);
			if (playerData != null)
			{
				csvData.Add(playerData);
			}
		}
		var path = Path.GetDirectoryName(typeof(AllPlayerDataToCsv).Assembly.Location) + "/export.csv";
		Export(path, csvData);
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

	private void Export(string path, List<Dictionary<string, string>> data)
	{
		using (var writer = new StreamWriter(path))
		using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
		{
			var headings = new List<string>(data.First().Keys);
			foreach (var heading in headings)
			{
				csv.WriteField(heading);
			}

			csv.NextRecord();
			foreach (var item in data)
			{
				foreach (var heading in headings)
				{
					csv.WriteField(item[heading]);
				}
				csv.NextRecord();
			}
		}
	}
}