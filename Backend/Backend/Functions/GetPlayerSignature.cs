using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using FirstLight.Game.Logic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Backend.Functions;

/// <summary>
/// Gets the player data signature. This signature can be used by other services to validate if 
/// the player data has been tampered.
/// </summary>
public class GetPlayerSignature
{
	private readonly ILogicWebService _gameLogicWebService;

	public GetPlayerSignature(ILogicWebService gameLogicWebService)
	{
		_gameLogicWebService = gameLogicWebService;
	}
	
	/// <summary>s
	/// Command Execution
	/// </summary>
	[FunctionName("GetPlayerSignature")]
	public async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
	                           HttpRequestMessage req, ILogger log)
	{
		var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
		return await _gameLogicWebService.GetPlayerDataSignature(context.AuthenticationContext.PlayFabId);
	}
}