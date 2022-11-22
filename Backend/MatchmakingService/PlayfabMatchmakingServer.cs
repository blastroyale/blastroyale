using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameLogicApp.Cloudscript;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using ServerCommon;

namespace Firstlight.Matchmaking
{
	/// <summary>
	/// Matchmaking server exposed functionality
	/// </summary>
	public interface IMatchmakingServer
	{
		/// <summary>
		/// Starts matchmaking by submiting a new ticket.
		/// </summary>
		public Task<CloudscriptResponse> StartMatchmaking(CloudscriptRequest request);

		/// <summary>
		/// Obtains all tickets for a given cloudscript request context.
		/// (e.g all open tickets user has)
		/// </summary>
		public Task<CloudscriptResponse> GetTickets(CloudscriptRequest request);

		/// <summary>
		/// Get state of a given ticket
		/// </summary>
		public Task<CloudscriptResponse> GetTicket(CloudscriptRequest request);

		/// <summary>
		/// Cancel and leave matchmaking
		/// </summary>
		public Task<CloudscriptResponse> LeaveMatchmaking(CloudscriptRequest request);
	}

	/// <summary>
	/// Object that represents the matchmaking server functionality.
	/// </summary>
	public class PlayfabMatchmakingServer : IMatchmakingServer
	{
		private readonly MatchmakingConfig _config;
		private readonly IErrorService<PlayFabError> _errorHandler;
		private readonly ILogger _log;

		public PlayfabMatchmakingServer(ILogger log, MatchmakingConfig config,
											IErrorService<PlayFabError> errorHandler)
		{
			_config = config;
			_errorHandler = errorHandler;
			_log = log;
		}

		public async Task<CloudscriptResponse> LeaveMatchmaking(CloudscriptRequest request)
		{
			try
			{
				await GetUserService(request).LeaveMatchmaking();
				return CloudscriptResponse.FromData(null);
			}
			catch (Exception e)
			{
				return CloudscriptResponse.FromError(e);
			}
		}

		public async Task<CloudscriptResponse> GetTickets(CloudscriptRequest request)
		{
			try
			{
				return CloudscriptResponse.FromObject(await GetUserService(request).GetTickets());
			}
			catch (Exception e)
			{
				return CloudscriptResponse.FromError(e);
			}
		}

		public async Task<CloudscriptResponse> GetTicket(CloudscriptRequest request)
		{
			try
			{
				var ticket = request.FunctionArgument.Data["ticket"];
				return CloudscriptResponse.FromObject(await GetUserService(request).GetTicket(ticket));
			}
			catch (Exception e)
			{
				return CloudscriptResponse.FromError(e);
			}
		}

		public async Task<CloudscriptResponse> StartMatchmaking(CloudscriptRequest request)
		{
			try
			{
				var userService = GetUserService(request);
				return CloudscriptResponse.FromData(new Dictionary<string, string>()
				{
					{"ticket", await userService.ObtainTicket()}
				});
			}
			catch (Exception e)
			{
				return CloudscriptResponse.FromError(e);
			}
		}

		private PlayfabUserMatchmakingService GetUserService(CloudscriptRequest request)
		{
			return new PlayfabUserMatchmakingService(_config, request.GetAuthContext(), _errorHandler);
		}
	}
}