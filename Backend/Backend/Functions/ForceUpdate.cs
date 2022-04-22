
#if DEBUG
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using Backend.Game;
using Backend.Game.Services;
using Backend.Models;
using FirstLight.Game.Logic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.Plugins.CloudScript;

namespace Backend.Functions;

/// <summary>
/// Forces playfab data update. Only usable in development.
/// Client will be able to send any data, and any data sent will be passed to the server state
/// without any type of validation.
/// </summary>
public class ForceUpdate
{
	private readonly GameServer _server;

	public ForceUpdate(GameServer server)
	{
		_server = server;
	}
	
	public async Task<dynamic> RunForceUpdate(FunctionContext<LogicRequest> context, ILogger log)
	{
		var playerId = context.AuthenticationContext.PlayFabId;
		log.Log(LogLevel.Information, $"Executing force update for {playerId}");
		var newState = new ServerState(context.FunctionArgument.Data);
		_server.State.UpdatePlayerState(playerId, newState);
		return new PlayFabResult<BackendLogicResult>
		{
			Error = null,
			CustomData = null,
			Result = new BackendLogicResult
			{
				PlayFabId = playerId,
				Command = context.FunctionArgument.Command,
				Data = newState
			}
		};
	}
	
	/// <summary>
	/// Command Execution
	/// </summary>
	[FunctionName("ForceUpdate")]
	public async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
	                                               HttpRequestMessage req, ILogger log)
	{
		if (!IsEnabled())
		{
			throw new LogicException("Force update not enabled");
		}
		var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
		return await RunForceUpdate(context, log);
	}
	
	private bool IsEnabled()
	{
		return Environment.GetEnvironmentVariable("FORCE_UPDATE", EnvironmentVariableTarget.Process) == "true"; 
	}
}
#endif
