using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using Quantum;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;


public class NoobTotalCount : PlayfabScript
{
	public override Environment GetEnvironment() => Environment.PROD;

	private ulong _noobAmount;
	
	public override void Execute(ScriptParameters parameters)
	{
		var task = RunAsync();
		task.Wait();
	}
	
	
	public async Task RunAsync()
	{
		var tasks = new List<Task>();
		var batchSize = 10000;
		var playerList = await GetPlayerSegmentByName("NOOB Total > 0");
		
		Console.WriteLine($"Total Players to Process: {playerList.Count}");
		var processedPlayers = 0;
		
		foreach (var player in playerList)
		{
			tasks.Add(Process(player));
			processedPlayers++;

			if (tasks.Count >= batchSize)
			{
				Console.WriteLine($"Processed Players: {processedPlayers}/{playerList.Count}");
				await Task.WhenAll(tasks.ToArray());
				
				Console.WriteLine("Waiting for 1 second before next batch...");

				
				await Task.Delay(1000);
				tasks.Clear(); 
			}
		}

		// Ensure any remaining tasks in the list are awaited before completing the method
		if (tasks.Count > 0)
		{
			Console.WriteLine($"Processing final batch, Processed Players: {processedPlayers}/{playerList.Count}");
			await Task.WhenAll(tasks.ToArray());
		}
		
		Console.WriteLine($"A total of {_noobAmount} NOOB Token was collected by {playerList.Count} players");	
		Console.WriteLine("All batches processed. Done!");
	}
	

	private async Task Process(PlayerProfile profile)
	{
		var state = await ReadUserState(profile.PlayerId);

		if (state != null)
		{
			var playerData = state.DeserializeModel<PlayerData>();
			var noobValue = playerData.Currencies[GameId.NOOB];

			Interlocked.Add(ref _noobAmount, noobValue);
		}
		else
		{
			Console.WriteLine($"Something went wrong when reading UserState for user {profile.PlayerId}");
			
		}
	}
}