using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using PlayFab;
using PlayFab.AdminModels;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

/// <summary>
/// Script to count guest players
/// </summary>
public class CountGuests : PlayfabScript
{

	private List<PlayerProfile> _guests = new ();
	private int _nonGuests = 0;
	
	public override Environment GetEnvironment() => Environment.PROD;
	
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
		Console.WriteLine("Guests: "+_guests.Count);
		Console.WriteLine("Non Guests: "+_nonGuests);
		Console.WriteLine("Guests User Ids:");
		Console.WriteLine(string.Join(",", _guests.Select(g => g.PlayerId)));
	}

	private async Task Proccess(PlayerProfile profile)
	{
		if (profile.ContactEmailAddresses.Count > 0)
		{
			_nonGuests++;
		}
		else
		{
			_guests.Add(profile);
		}
		Console.Write("Processed player "+profile.PlayerId);
	}
}