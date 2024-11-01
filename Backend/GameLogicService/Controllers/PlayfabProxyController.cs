using System;
using System.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PlayFab;
using PlayFab.CloudScriptModels;

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
		private IServiceProvider _provider;
		private ILogger _log;
		private IBaseServiceConfiguration _config;

		private class PlayfabProxyResponseFormat
		{
			public int code;
			public string status;
			public object data;
		}

		public PlayfabProxyController(IServiceProvider provider, ILogger log, IBaseServiceConfiguration config)
		{
			_provider = provider;
			_log = log;
			_config = config;
		}
		
		[HttpPost]
		[Route("ExecuteFunction")]
		public async Task<dynamic> ExecuteFunction([FromBody] ExecuteFunctionRequest functionRequest)
		{
			if (!_config.DevelopmentMode) return Unauthorized();
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			var playerId = functionRequest?.AuthenticationContext.PlayFabId;
			var logicString = functionRequest?.FunctionParameter as JObject;
			var model = new CloudscriptRequest<LogicRequest>(playerId)
			{
				FunctionArgument = serializer.DeserializeObject<LogicRequest>(logicString?.ToString())
			};
			_log.LogInformation($"Proxy Request received for function {functionRequest.FunctionName} with argument "+logicString);
			var controller = _provider.GetService(typeof(CloudscriptController)) as CloudscriptController;
			dynamic response = controller.GetType().GetMethod("Generic").Invoke(controller, new object [] {model});
			var result = response.GetAwaiter().GetResult();
			return Content(serializer.SerializeObject(new PlayfabProxyResponseFormat()
			{
				code = 200,
				status = "OK",
				data = new ExecuteFunctionResult()
				{
					FunctionName = functionRequest.FunctionName,
					FunctionResult = new PlayFabResult<BackendLogicResult>()
					{
						Result = ((CloudscriptResponse)result.Value).Result
					}
				}
			}), "application/json");
		}
	}
}

