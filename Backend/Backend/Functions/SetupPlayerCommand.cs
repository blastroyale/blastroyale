using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PlayFab;


namespace Backend.Functions;

/// <summary>
/// This command is executed by by the client when a player is created in the game.
/// It setups the basic game backend data for the player.
/// </summary>
public class SetupPlayerCommand
{
	private readonly IPlayerSetupService _setupService;
	private readonly IServerStateService _stateService;
	
	public SetupPlayerCommand(IPlayerSetupService service, IServerStateService stateService)
	{
		_setupService = service;
		_stateService = stateService;
	}
	/// <summary>
	/// Command Execution
	/// </summary>
	[FunctionName("SetupPlayerCommand")]
	public async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
	                                                                    HttpRequestMessage req, ILogger log)
	{
		var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
		var serverData = _setupService.GetInitialState(context.AuthenticationContext.PlayFabId);
		_stateService.UpdatePlayerState(context.AuthenticationContext.PlayFabId, serverData);
		return new PlayFabResult<LogicResult>
		{
			Result = new LogicResult
			{
				PlayFabId = context.AuthenticationContext.PlayFabId,
				Data = serverData
			}
		};
	}
}