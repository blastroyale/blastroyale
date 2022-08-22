using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Backend.Functions
{
	/// <summary>
	/// Sets the player state to its initial state.
	/// </summary>
	public class GetPlayerDataCommand
	{
		private readonly ILogicWebService _gameLogicWebService;

		public GetPlayerDataCommand(ILogicWebService gameLogicWebService)
		{
			_gameLogicWebService = gameLogicWebService;
		}
	
		/// <summary>
		/// Command Execution
		/// </summary>
		[FunctionName("GetPlayerData")]
		public async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		                                    HttpRequestMessage req, ILogger log)
		{
			var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
			return await _gameLogicWebService.GetPlayerData(context.AuthenticationContext.PlayFabId);
		}
	}
}

