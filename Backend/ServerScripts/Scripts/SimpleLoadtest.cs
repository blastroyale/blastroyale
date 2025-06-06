using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic.RPC;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using GetPlayerCombinedInfoRequestParams = PlayFab.ClientModels.GetPlayerCombinedInfoRequestParams;

/// <summary>
/// This is the minimal form of a loadtest. This was done as opposed to implementing it via JMX file due to
/// time constraints. Ideally we would like to migrate this to a JMX to have distributed load testing for more than 300 CCU
/// This load test can take an average of 200 CCU in a 2021 Macbook Pro
/// </summary>
public class SimpleLoadtest : PlayfabScript
{
	public override Environment GetEnvironment() => Environment.DEV;

	public const int NUMBER_OF_PLAYERS = 3;
	
	public override void Execute(ScriptParameters parameters)
	{
		Console.WriteLine($"Running for {NUMBER_OF_PLAYERS} players");
		var tasks = new List<Task<LoginResult>>();
		for (var x = 0; x < 10; x++)
		{
			for (var y = 0; y < NUMBER_OF_PLAYERS; y++)
			{
				tasks.Add(CreatePlayer());
			}
			Task.WaitAll(tasks.ToArray());
		}

		var main = RunAsync(tasks.Select(t => t.Result).ToList());
		main.Wait();
	}

	public async Task RunAsync(List<LoginResult> loggedInUsers)
	{
		var cmd = new CollectUnclaimedRewardsCommand();
		var start = DateTime.UtcNow;
		var tasks = new List<Task>();
		
		foreach (var loggedIn in loggedInUsers)
		{
			tasks.Add(GetPlayerData(loggedIn));
			tasks.Add(SubmitCommand(loggedIn, cmd));
		}
		
		Task.WaitAll(tasks.ToArray());
		
		var end = DateTime.UtcNow;
		var elapsed = (end - start).TotalMilliseconds;
		Console.WriteLine("Done !");
		Console.WriteLine($"Elapsed Milliseconds: {elapsed}");
	}

	private async Task<LoginResult> CreatePlayer()
	{
		var result = await PlayFabClientAPI.LoginWithCustomIDAsync(new LoginWithCustomIDRequest()
		{
			CreateAccount = true,
			CustomId = Guid.NewGuid().ToString(),
			TitleId = GetPlayfabConfiguration().TitleId,
			InfoRequestParameters = new GetPlayerCombinedInfoRequestParams()
			{
				GetUserAccountInfo = true
			}
		});
		HandleError(result.Error);
		Console.WriteLine($"Player {result.Result.PlayFabId} created");
		return result.Result;
	}
	
	private async Task GetPlayerData(LoginResult login)
	{
		var res = CallFunctionAsPlayer("GetPlayerData", login);
		Console.WriteLine($"Player {login.PlayFabId} read its data from server");
	}

	private async Task SubmitCommand(LoginResult login, IGameCommand cmd)
	{
		var cmdName = cmd.GetType().FullName;
		var cmdData = ModelSerializer.Serialize(cmd).Value;
		var logicRequest = new LogicRequest()
		{
			Command = cmdName,
			Data = new Dictionary<string, string>
			{
				{CommandFields.CommandType, cmdData},
				{CommandFields.Timestamp, DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()},
				{CommandFields.ClientVersion, "100000.0.0"}
			}
		};
		await CallFunctionAsPlayer("ExecuteCommand", login, logicRequest);
		Console.WriteLine($"Player {login.PlayFabId} sent command {cmd.GetType().Name}");
	}
	
	private async Task<PlayFabResult<ExecuteFunctionResult>> CallFunctionAsPlayer(string functionName, LoginResult login, object param=null)
	{
		var entity = login.EntityToken;
		var res = await PlayFabCloudScriptAPI.ExecuteFunctionAsync(new ExecuteFunctionRequest()
		{
			Entity = new PlayFab.CloudScriptModels.EntityKey()
			{
				Id = entity.Entity.Id,
				Type = entity.Entity.Type
			},
			AuthenticationContext = new PlayFabAuthenticationContext()
			{
				EntityId = entity.Entity.Id,
				EntityToken = entity.EntityToken,
				EntityType = entity.Entity.Type,
				ClientSessionTicket = login.SessionTicket,
				PlayFabId = login.PlayFabId
			},
			FunctionName = functionName,
			FunctionParameter = param
		});
		HandleError(res.Error);
		return res;
	}
}