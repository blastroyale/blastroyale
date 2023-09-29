using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Backend;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using GameLogicService.Game;
using GameLogicService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using ServerCommon.Authentication.ApiKey;

namespace ServerCommon.Cloudscript
{
	/// <summary>
	/// Acts as a playfab proxy. Will receive requests like playfab would and route to our Cloudscript controller
	/// like playfab would.
	/// its also free
	/// like playfab is
	/// </summary>
	[ApiController]
	[Route("cloudscript")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class PlayfabProxyController : ControllerBase
	{
		private IStatisticsService _statistics;
		private ILogicWebService _logicServer;
		private ILogger _log;
		private IBaseServiceConfiguration _config;
		private ShopService _shop;

		// Playfab Format for response
		[Serializable]
		private class PlayfabHttpResponse
		{
			public int code;
			public string status;
			public PlayfabFunctionResult data;
		}
		
		[Serializable]
		private class PlayfabFunctionResult
		{
			public FunctionExecutionError Error;
			public int ExecutionTimeMilliseconds;
			public string FunctionName;
			public PlayFabResult<BackendLogicResult> FunctionResult;
			public bool? FunctionResultTooLarge;
		}
		
		public PlayfabProxyController(ILogicWebService logicServer, ShopService shop, ILogger log, IStatisticsService stats, IBaseServiceConfiguration config)
		{
			_logicServer = logicServer;
			_log = log;
			_config = config;
			_statistics = stats;
			_shop = shop;
		}
		
		[HttpPost]
		[Route("ExecuteFunction")]
		public async Task<dynamic> ExecuteFunction([FromBody] ExecuteFunctionRequest functionRequest)
		{
			if (!_config.DevelopmentMode) return Unauthorized();
			_log.LogInformation($"Proxy Request received for function {functionRequest.FunctionName}");
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			var playerId = functionRequest?.AuthenticationContext.PlayFabId;
			var logicString = functionRequest?.FunctionParameter as JObject;
			var logicRequest = serializer.DeserializeObject<LogicRequest>(logicString?.ToString());
			PlayFabResult<BackendLogicResult> result = functionRequest?.FunctionName switch
			{
				"ConsumeValidatedPurchaseCommand" => await _shop.ProcessPurchaseRequest(playerId, logicRequest.Data["item_id"], bool.Parse(logicRequest.Data["fake_store"])),
				"RemovePlayerData"                => await _logicServer.RemovePlayerData(playerId),
				"ExecuteCommand"                  => await _logicServer.RunLogic(playerId, logicRequest),
				"GetPlayerData"                   => await _logicServer.GetPlayerData(playerId),
				"GetPublicProfile"                => Playfab.Result(playerId, await _statistics.GetProfile(logicRequest.Command)),
				_                                 => throw new ArgumentOutOfRangeException()
			};
			return Content(serializer.SerializeObject(new PlayfabHttpResponse()
			{
				code = 200,
				status = "OK",
				data = new PlayfabFunctionResult()
				{
					FunctionName = "ExecuteCommand",
					FunctionResult = result
				}
			}), "application/json"); 
		}
	}
}

