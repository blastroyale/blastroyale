using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using PlayFab;
using PlayFab.AdminModels;
using Unity.Services.CloudSave.Admin.Model;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

/// <summary>
/// Script to count guest players
/// </summary>
public class SetName : PlayfabScript
{
	public override Environment GetEnvironment() => Environment.DEV;

	public override void Execute(ScriptParameters parameters)
	{
		var task = RunAsync();
		task.Wait();
	}

	public async Task RunAsync()
	{
		var playerId = "L1sxKRp3qBUdm1kHWCgQu3kdEm34";
		var ctx = GetUnityContext();
		await GetUnityAdmin().CloudSaveData.SetCustomItemAsync(
			ctx,
			ctx.ServiceAccountKeyId,
			ctx.ServiceAccountSecretKey,
			ctx.ProjectGuid,
			ctx.EnvironmentGuid,
			"friend-"+playerId,
			new SetItemBody("player_name","TestValue"));
	}
}