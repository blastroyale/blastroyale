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
	private readonly IConfigsProvider _cfg;
	private readonly IServerStateService _serverState;
	private readonly IServerCommahdHandler _serverCommandHandler;
	
	public ExecuteCommand(IConfigsProvider cfg, IServerStateService serverState, IServerCommahdHandler commands)
	{
		_cfg = cfg;
		_serverState = serverState;
		_serverCommandHandler = commands;
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
			return await new ForceUpdate(_serverState).RunForceUpdate(context, log);
		}
		#endregion
		
	
		var cmdType = context.FunctionArgument.Command;
		var cmdData = context.FunctionArgument.Data;
		var commandInstance = _serverCommandHandler.BuildCommandInstance(cmdData, cmdType);
		log.Log(LogLevel.Information, $"Player {playerId} running server command {commandInstance.GetType().Name}");
		var newState = RunCommand(playerId, commandInstance);
		return new PlayFabResult<LogicResult>
		{
			Result = new LogicResult
			{
				PlayFabId = playerId,
				Command = context.FunctionArgument.Command,
				Data = newState
			}
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

	/// <summary>
	/// Executes the command on the server and updates the server state.
	/// Returns the new state after the logic updates.
	/// </summary>
	private ServerState RunCommand(string playerId, IGameCommand commandInstance)
	{
		var currentPlayerState = _serverState.GetPlayerState(playerId);
		var newState = _serverCommandHandler.ExecuteCommand(commandInstance, currentPlayerState);
		_serverState.UpdatePlayerState(playerId, newState);
		return newState;
	}

}
