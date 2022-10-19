using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using FirstLight.Game.Logic.RPC;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Backend.Functions
{
	/// <summary>
	/// Function to remove player data. Implemented as function to keep backwards compatibility
	/// on staging server currently. Will be deleted soon.
	/// </summary>
	public class RemovePlayerDataFunction
	{
		private ILogicWebService _server;
	
		public RemovePlayerDataFunction(ILogicWebService server)
		{
			_server = server;
		}
		
		[FunctionName("RemovePlayerData")]
		public async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, ILogger log)
		{
			var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
			var playerId = context.AuthenticationContext.PlayFabId;
			return await _server.RemovePlayerData(playerId);
		}
	}
}

