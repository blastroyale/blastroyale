using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Backend;
using FirstLight.Game.Logic;
using GameLogicService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using ServerShared.Authentication.ApiKey;

namespace ContainerApp.Cloudscript
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

		public CloudscriptController(ILogicWebService logicServer, ShopService shop)
		{
			_logicServer = logicServer;
			_shop = shop;
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("ConsumeValidatedPurchaseCommand")]
		public async Task<dynamic> ConsumeValidatedPurchaseCommand([FromBody] CloudscriptRequest request)
		{
			var itemId = request.FunctionArgument.Data["item_id"];
			return Ok(new CloudscriptResponse(await _shop.ProcessPurchaseRequest(request.PlayfabId, itemId)));
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("ExecuteCommand")]
		public async Task<dynamic> ExecuteCommand([FromBody] CloudscriptRequest request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.RunLogic(request.PlayfabId, request.FunctionArgument)));
		}
	
		[HttpPost]
		[RequiresApiKey]
		[Route("GetPlayerData")]
		public async Task<IActionResult> GetPlayerData([FromBody] CloudscriptRequest request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.GetPlayerData(request.PlayfabId)));
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("RemovePlayerData")]
		public async Task<IActionResult> DeletePlayerData([FromBody] CloudscriptRequest request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.RemovePlayerData(request.PlayfabId)));
		}
	}
}

