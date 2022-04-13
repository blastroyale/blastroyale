using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using FirstLight.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.ServerModels;

namespace Backend.Functions;

/// <summary>
/// This command only exists for debugging purposes to allow to reset the player data to it's initial values
/// </summary>
public class PlayStreamSetupCommand
{
	private readonly IPlayerSetupService _setupService;
	private readonly PlayfabGameStateService _stateService;
	
	public PlayStreamSetupCommand(IPlayerSetupService service, IServerStateService stateService)
	{
		_setupService = service;
		_stateService = (PlayfabGameStateService)stateService; // TODO: Fix cast  when server env setup is done
	}
	
	/// <summary>
	/// Command Execution
	/// </summary>
	[FunctionName("PlayStreamSetupCommand")]
	public async Task RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
	                                  HttpRequestMessage req, ILogger log)
	{
		var context = await ContextProcessor.ProcessPlayStreamContext(req);
		var serverData = _setupService.GetInitialState(context.PlayFabId);
		_stateService.UpdatePlayerState(context.PlayFabId, serverData);
	}
}