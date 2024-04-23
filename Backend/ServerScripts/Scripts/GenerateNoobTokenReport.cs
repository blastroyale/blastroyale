using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Models;
using Quantum;
using Scripts.Base;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

/// <summary>
/// Script to wipe BPP
/// </summary>
public class GenerateNoobTokenReport : PlayfabScript
{
	public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.DEV;

	private HubService _hubService;
	
	private List<Dictionary<string, string>> _csvData;
	
	public override void Execute(ScriptParameters parameters)
	{
		_hubService = new HubService();
		_csvData = new List<Dictionary<string, string>>();
		
		var task = RunAsync();
		task.Wait();
	}
	
	
	public async Task RunAsync()
	{
		var tasks = new List<Task>();
		var batchSize = 10000;
		var playerList = await GetPlayerSegmentByName("OwnNoobs");
		foreach (var player in playerList)
		{
			tasks.Add(Process(player));
			if (tasks.Count >= batchSize)
			{
				Console.WriteLine("Waiting Batch Process");
				await Task.WhenAll(tasks.ToArray());
				tasks.Clear();
			}
		}

		Task.WaitAll(tasks.ToArray());
		
		GenerateInGameNoobOwnedReport();
		
		Console.WriteLine("Done !");
	}
	

	private void GenerateInGameNoobOwnedReport()
	{
		var path = Path.GetDirectoryName(typeof(AllPlayerDataToCsv).Assembly.Location) + "/playerid-noobtoken.csv";
		FileUtil.ExportToCsv(path, _csvData);
	}

	private async Task Process(PlayerProfile profile)
	{
		var state = await ReadUserState(profile.PlayerId);

		if (state != null)
		{
			var playerData = state.DeserializeModel<PlayerData>();
				
			var currencyData = new Dictionary<string, string>();
			currencyData["playerId"] = profile.PlayerId;
			currencyData["walletAddress"] = await _hubService.FetchWalletAddressFromPlayerIdAsync(profile.PlayerId);
			currencyData["noobTokens"] = playerData.Currencies[GameId.NOOB].ToString();
						
			_csvData.Add(currencyData);
		}
	}
	
}