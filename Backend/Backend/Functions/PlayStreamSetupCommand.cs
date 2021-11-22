using System.Net.Http;
using System.Threading.Tasks;
using Backend.Models;
using Backend.Util;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PlayFab;

namespace Backend.Functions
{
	/// <summary>
	/// This command only exists for debugging purposes to allow to reset the player data to it's initial values
	/// </summary>
	public static class PlayStreamSetupCommand
	{
		/// <summary>
		/// Command Execution
		/// </summary>
		[FunctionName("PlayStreamSetupCommand")]
		public static async Task RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		                                  HttpRequestMessage req, ILogger log)
		{
			var context = await ContextProcessor.ProcessPlayStreamContext(req);
			var server = new PlayFabServerInstanceAPI(context.ApiSettings, context.AuthenticationContext);
			var request = SetupPlayerCommand.GetInitialDataRequest(context.PlayFabId);
			
			log.Log(LogLevel.Information, $"{request.PlayFabId} is executing - PlayStreamSetupCommand");
			
			var result = await server.UpdateUserReadOnlyDataAsync(request);

			if (result.Error != null)
			{
				throw new LogicException($"PlayStreamSetupCommand error: {request.PlayFabId} - {result.Error.GenerateErrorReport()}");
			}
		}
	}
}