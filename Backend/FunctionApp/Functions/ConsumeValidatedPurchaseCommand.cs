using System.Net.Http;
using System.Threading.Tasks;
using Backend.Context;
using FirstLight.Game.Logic.RPC;
using GameLogicService.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Backend.Functions
{
	/// <summary>
	/// This is the end point of the client backend execution commands.
	/// The FunctionApp only exist to validate the game logic that is already executing in the backend.
	/// </summary>
	public class ConsumeValidatedPurchaseCommand
	{
		private ShopService _shop;
	
		public ConsumeValidatedPurchaseCommand(ShopService shop)
		{
			_shop = shop;
		}
		
		[FunctionName("ConsumeValidatedPurchaseCommand")]
		public async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, ILogger log)
		{
			var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
			var playerId = context.AuthenticationContext.PlayFabId;
			return await _shop.ProcessPurchaseRequest(playerId, context.FunctionArgument.Data["item_id"]);
		}
	}
}

