using System;
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
	private readonly ILogicWebService _gameLogicWebService;

	public PlayStreamSetupCommand(ILogicWebService gameLogicWebService)
	{
		_gameLogicWebService = gameLogicWebService;
	}
	
	/// <summary>
	/// Command Execution
	/// </summary>
	[FunctionName("PlayStreamSetupCommand")]
	public async Task RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
	                                  HttpRequestMessage req, ILogger log)
	{
		throw new Exception("Playstream not implemented, yet");
	}
}