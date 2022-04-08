using System.Net.Http;
using System.Threading.Tasks;
using Backend.Models;
using Backend.Util;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;

namespace Backend.Functions
{
	/// <summary>
	/// This is the end point of the client backend execution commands.
	/// The Backend only exist to validate the game logic that is already executing in the backend.
	/// </summary>
	public static class ExecuteCommand
	{
		/// <summary>
		/// Command Execution
		/// </summary>
		[FunctionName("ExecuteCommand")]
		public static async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		                                               HttpRequestMessage req, ILogger log)
		{
			var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
			var server = new PlayFabServerInstanceAPI(context.ApiSettings, context.AuthenticationContext);

			var request = new UpdateUserDataRequest
			{
				Data = context.FunctionArgument.Data, 
				PlayFabId = context.AuthenticationContext.PlayFabId,
				KeysToRemove = context.FunctionArgument.RemoveKeys
			};
			
			log.Log(LogLevel.Information, $"{request.PlayFabId} is executing - {context.FunctionArgument.Command}");

			var result = await server.UpdateUserReadOnlyDataAsync(request);

			return new PlayFabResult<LogicResult>
			{
				CustomData = result.CustomData,
				Error = result.Error,
				Result = new LogicResult
				{
					PlayFabId = request.PlayFabId,
					Command = context.FunctionArgument.Command,
					Data = context.FunctionArgument.Data
				}
			};
		}
	}
}