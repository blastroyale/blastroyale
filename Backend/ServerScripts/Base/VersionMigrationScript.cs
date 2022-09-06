using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;

/// <summary>
/// Will setup playfab before script run
/// </summary>
public abstract class VersionMigrationScript : PlayfabScript
{
	
	/// <summary>
	/// Should return which version this applies to
	/// </summary>
	public abstract Version VersionApplied();
	
	/// <summary>
	/// Should perform the data migration and modify the state object
	/// </summary>
	public abstract Task<bool> MigrateData(string playerId, ServerState state);

	public override void Execute(ScriptParameters args)
	{
		ExecuteAsync().Wait();
	}

	private async Task ProccessUserTask(PlayerProfile profile)
	{
		var state = await ReadUserState(profile.PlayerId);
		if (state != null && await MigrateData(profile.PlayerId, state))
		{
			Console.WriteLine($"Migrating user {profile.PlayerId}");
			await SetUserState(profile.PlayerId, state);
		}
	}

	private async Task ExecuteAsync()
	{
		List<Task> tasks = new List<Task>();
		foreach (var player in await this.GetAllPlayers())
		{
			tasks.Add(ProccessUserTask(player));
		}
		await Task.WhenAll(tasks);
	}
}