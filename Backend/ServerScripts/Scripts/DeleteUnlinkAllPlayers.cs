using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.AdminModels;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

/// <summary>
/// Will delete all players and unlink their accounts from marketplace.
/// </summary>
public class DeleteAndUnlinkAllPlayers : PlayfabScript
{
	public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.STAGING;
	
	private HttpClient _client;
	
	public override void Execute(ScriptParameters parameters)
	{
		_client = new HttpClient();
		var task = RunAsync();
		task.Wait();
	}

	public async Task RunAsync()
	{
		var tasks = new List<Task>();
		foreach (var player in await GetAllPlayers())
		{
			tasks.Add(DeletingPlayer(player));
		}
		Task.WaitAll(tasks.ToArray());
		
		var unlinkResult = await _client.DeleteAsync($"https://apim-marketplace-flg-{GetEnvironment().ToString().ToLower()}.azure-api.net/accounts/admin/unlinkall?key=devkey");
		if (unlinkResult.StatusCode != HttpStatusCode.OK)
		{
			var result = await unlinkResult.Content.ReadAsStringAsync();
			Console.WriteLine($"Error unlinking player wallet {unlinkResult.StatusCode.ToString()} {result}");
		}
		else
		{
			Console.WriteLine($"Unlinked player wallets");
		}
		Console.WriteLine("Done !");
	}

	private async Task DeletingPlayer(PlayerProfile profile)
	{
		Console.WriteLine($"Processing player {profile.PlayerId}");
		var result = await PlayFabAdminAPI.DeletePlayerAsync(new DeletePlayerRequest()
		{
			PlayFabId = profile.PlayerId
		});

		Console.WriteLine($"Deleted from playfab player {profile.PlayerId}");
	}
}