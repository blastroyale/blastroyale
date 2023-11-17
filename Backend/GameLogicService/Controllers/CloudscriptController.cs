using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Backend;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLightServerSDK.Services;
using GameLogicService.Game;
using GameLogicService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using ServerCommon.Authentication.ApiKey;

namespace ServerCommon.Cloudscript
{
	/// <summary>
	/// Controller that uses playfab signature API to communicate with playfab cloud script.
	/// </summary>
	[ApiController]
	[Route("cloudscript")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class CloudscriptController : ControllerBase
	{
		private IStatisticsService _statistics;
		private ILogicWebService _logicServer;
		private ShopService _shop;
		private IEventManager _events;

		public CloudscriptController(ILogicWebService logicServer, ShopService shop, IEventManager events, IStatisticsService stats)
		{
			_logicServer = logicServer;
			_shop = shop;
			_events = events;
			_statistics = stats;
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("ConsumeValidatedPurchaseCommand")]
		public async Task<dynamic> ConsumeValidatedPurchaseCommand([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			var itemId = request.FunctionArgument.Data["item_id"];
			return Ok(new CloudscriptResponse(await _shop.ProcessPurchaseRequest(request.PlayfabId, itemId)));
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("ExecuteCommand")]
		public async Task<dynamic> ExecuteCommand([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.RunLogic(request.PlayfabId, request.FunctionArgument)));
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("SyncPlayfabInventory")]
		public async Task<dynamic> ExecuteEvent([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			await _events.CallEvent(new InventoryUpdatedEvent(request.PlayfabId));
			return Ok(new CloudscriptResponse(Playfab.Result(request.PlayfabId)));
		}
	
		[HttpPost]
		[RequiresApiKey]
		[Route("GetPlayerData")]
		public async Task<IActionResult> GetPlayerData([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.GetPlayerData(request.PlayfabId)));
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("RemovePlayerData")]
		public async Task<IActionResult> DeletePlayerData([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.RemovePlayerData(request.PlayfabId)));
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("GetPublicProfile")]
		public async Task<IActionResult> GetUserProfile([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			var result = Playfab.Result(request.PlayfabId, await _statistics.GetProfile(request.FunctionArgument!.Command));
			return Ok(new CloudscriptResponse(result));
		}
	}
}

