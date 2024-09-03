using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Backend;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Modules.Commands;
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
		private UnityAuthService _unityAuthService;
		private UnityCloudService _unityCloudService;


		delegate Task<IActionResult> RunActionDelegate(CloudscriptRequest<LogicRequest> request);

		private Dictionary<string, RunActionDelegate> _functionDelegates;

		public CloudscriptController(ILogicWebService logicServer, ShopService shop, IEventManager events, IStatisticsService stats, UnityAuthService unityAuthService, UnityCloudService unityCloudService)
		{
			_logicServer = logicServer;
			_shop = shop;
			_events = events;
			_statistics = stats;
			_unityAuthService = unityAuthService;
			_unityCloudService = unityCloudService;
			InitializeDelegates();
		}

		private void InitializeDelegates()
		{
			_functionDelegates = new Dictionary<string, RunActionDelegate>()
			{
				{ CommandNames.EXECUTE_LOGIC, ExecuteCommand },
				{ CommandNames.GET_PLAYER_DATA, GetPlayerData },
				{ CommandNames.GET_PLAYER_PROFILE, GetPublicProfile },
				{ CommandNames.AUTHENTICATE_UNITY, AuthenticateUnity },
				{ CommandNames.CONSUME_VALIDATE_PURCHASE, ConsumeValidatedPurchaseCommand },
				{ CommandNames.SYNC_NAME, SyncName },
				{ CommandNames.REMOVE_PLAYER_DATA, RemovePlayerData },
			};
		}

		[HttpPost]
		[RequiresApiKey]
		[Route("Generic")]
		public Task<IActionResult> Generic([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			if (request.FunctionArgument != null && _functionDelegates.TryGetValue(request.FunctionArgument.Command, out var @delegate))
			{
				return @delegate.Invoke(request);
			}

			return Task.FromResult<IActionResult>(BadRequest("invalid function argument"));
		}

		public async Task<IActionResult> ConsumeValidatedPurchaseCommand([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			var itemId = request.FunctionArgument.Data["item_id"];
			return Ok(new CloudscriptResponse(await _shop.ProcessPurchaseRequest(request.PlayfabId, itemId)));
		}

		public async Task<IActionResult> ExecuteCommand([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.RunLogic(request.PlayfabId, request.FunctionArgument)));
		}


		public async Task<IActionResult> GetPlayerData([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.GetPlayerData(request.PlayfabId)));
		}

		public async Task<IActionResult> AuthenticateUnity([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			var customID = await _unityAuthService.AuthenticateCustomIdRequest(request.PlayfabId);
			return Ok(new CloudscriptResponse(Playfab.Result(request.PlayfabId, new Dictionary<string, string>
			{
				{
					"idToken", customID.idToken
				},
				{
					"sessionToken", customID.sessionToken
				},
			})));
		}

		public async Task<IActionResult> RemovePlayerData([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			return Ok(new CloudscriptResponse(await _logicServer.RemovePlayerData(request.PlayfabId)));
		}

		public async Task<IActionResult> GetPublicProfile([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			if (request.FunctionArgument?.Data.TryGetValue(CommandFields.PlayerId, out var playerId) ?? false)
			{
				var result = Playfab.Result(request.PlayfabId, await _statistics.GetProfile(playerId));
				return Ok(new CloudscriptResponse(result));
			}

			return BadRequest("Missing player id!");
		}

		public async Task<IActionResult> SyncName([FromBody] CloudscriptRequest<LogicRequest> request)
		{
			await _unityCloudService.SyncName(request.PlayfabId);
			var result = Playfab.Result(request.PlayfabId, true);
			return Ok(new CloudscriptResponse(result));
		}
	}
}