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
		private ILogicWebService _logicServer;
		private ShopService _shop;
		private IEventManager _events;

		public CloudscriptController(ILogicWebService logicServer, ShopService shop, IEventManager events)
		{
			_logicServer = logicServer;
			_shop = shop;
			_events = events;
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("ConsumeValidatedPurchaseCommand")]
		public async Task<dynamic> ConsumeValidatedPurchaseCommand([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			var itemId = request.FunctionArgument.Data["item_id"];
			var fakeStore = bool.Parse(request.FunctionArgument.Data["fake_store"]);
			return Ok(new CloudscriptResponse(await _shop.ProcessPurchaseRequest(request.PlayfabId, itemId, fakeStore)));
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
	}
}

