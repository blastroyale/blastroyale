using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using Backend.Game;
using Backend.Game.Services;
using Backend.Models;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.Plugins.CloudScript;

namespace Backend.Functions;

/// <summary>
/// This is the end point of the client backend execution commands.
/// The Backend only exist to validate the game logic that is already executing in the backend.
/// </summary>
public class ExecuteCommand
{
	private GameServer _server;
	
	public ExecuteCommand(GameServer server)
	{
		_server = server;
	}

	/// <summary>
	/// Command Execution
	/// </summary>
	[FunctionName("ExecuteCommand")]
	public async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
	                                               HttpRequestMessage req, ILogger log)
	{
		// TODO: Player semaphore check for atomicity
		var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
		var playerId = context.AuthenticationContext.PlayFabId;

		log.Log(LogLevel.Information, $"Keys {string.Join(",", context.FunctionArgument.Data.Keys)}");
		
		#region Backwards Compatibility Hack
		// TODO: Remove this hack
		if (ForceUpdateBackwardsCompability(context))
		{
			log.Log(LogLevel.Information, $"Running backwards compatible force update for {playerId}");
			return await new ForceUpdate(_server).RunForceUpdate(context, log);
		}
		#endregion

		var logicRequest = context.FunctionArgument;
		var result = _server.RunLogic(playerId, logicRequest);
		return new PlayFabResult<BackendLogicResult>
		{
			Result = result
		};
	}

	/// <summary>
	/// This function will be removed soon and its just for backwards compatibility.
	/// TODO: REMOVE ME !! 
	/// </summary>
	private bool ForceUpdateBackwardsCompability(FunctionContext<LogicRequest> context)
	{
		var data = context.FunctionArgument.Data;
		return data.ContainsKey(nameof(PlayerData)) || data.ContainsKey(typeof(PlayerData).FullName);
	}


}
