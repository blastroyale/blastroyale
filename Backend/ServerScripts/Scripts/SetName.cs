using System.Threading.Tasks;
using Unity.Services.CloudSave.Admin.Model;

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