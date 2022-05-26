using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using FirstLight.Game.Logic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Backend.Functions;

/// <summary>
/// Sets the player state to its initial state.
/// </summary>
public class SetupPlayerCommand
{
	private readonly ILogicWebService _gameLogicWebService;

	public SetupPlayerCommand(ILogicWebService gameLogicWebService)
	{
		_gameLogicWebService = gameLogicWebService;
	}
	
	/// <summary>
	/// Command Execution
	/// </summary>
	[FunctionName("SetupPlayerCommand")]
	public async Task RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
	                           HttpRequestMessage req, ILogger log)
	{
		var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
		await _gameLogicWebService.SetupPlayer(context.AuthenticationContext.PlayFabId);
	}
}