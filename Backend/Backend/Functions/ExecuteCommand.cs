using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using FirstLight.Game.Logic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Backend.Functions;

/// <summary>
/// This is the end point of the client backend execution commands.
/// The Backend only exist to validate the game logic that is already executing in the backend.
/// </summary>
public class ExecuteCommand
{
	private ILogicWebService _server;
	
	public ExecuteCommand(ILogicWebService server)
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
		var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
		var playerId = context.AuthenticationContext.PlayFabId;
		return _server.RunLogic(playerId, context.FunctionArgument);
	}
}
