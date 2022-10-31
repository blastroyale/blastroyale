using System;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.AuthenticationModels;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using ServerCommon;
using BindingFlags = PlayFab.BindingFlags;
using EntityKey = PlayFab.MultiplayerModels.EntityKey;


namespace Firstlight.Matchmaking
{
	/// <summary>
	/// Matchmaking setup to matchmake users.
	/// The service wraps functionality to enable matchmaking to happen for a given specific player.
	/// </summary>
	public interface IUserMatchmakingService
	{
		public Task LeaveMatchmaking();

		public Task<GetMatchmakingTicketResult> GetTicket(string ticket);

		public Task<ListMatchmakingTicketsForPlayerResult> GetTickets();

		public Task<string> ObtainTicket();
	}

	public class PlayfabUserMatchmakingService : IUserMatchmakingService
	{
		private readonly MatchmakingConfig _config;
		private PlayFabAuthenticationContext _userContext;
		private IErrorService<PlayFabError> _errorHandler;
		private PlayFabMultiplayerInstanceAPI _userApi;

		public PlayfabUserMatchmakingService(MatchmakingConfig config, PlayFabAuthenticationContext ctx,
											 IErrorService<PlayFabError> errorHandler)
		{
			_config = config;
			_errorHandler = errorHandler;
			_userContext = ctx;
			_userApi = new PlayFabMultiplayerInstanceAPI(ctx);
		}

		public async Task LeaveMatchmaking()
		{
			var entity = new EntityKey()
			{
				Id = _userContext.EntityId,
				Type = _userContext.EntityType
			};
			var result = await _userApi.CancelAllMatchmakingTicketsForPlayerAsync(new()
			{
				Entity = entity,
				QueueName = _config.QueueName
			});
			_errorHandler.CheckErrors(result);
		}

		public async Task<GetMatchmakingTicketResult> GetTicket(string ticket)
		{
			var result = await _userApi.GetMatchmakingTicketAsync(new()
			{
				QueueName = _config.QueueName,
				TicketId = ticket,
			});
			_errorHandler.CheckErrors(result);
			return result.Result;
		}

		public async Task<ListMatchmakingTicketsForPlayerResult> GetTickets()
		{
			var result = await _userApi.ListMatchmakingTicketsForPlayerAsync(new()
			{
				Entity = new EntityKey()
				{
					Id = _userContext.EntityId,
					Type = _userContext.EntityType
				},
				QueueName = _config.QueueName
			});
			_errorHandler.CheckErrors(result);
			return result.Result;
		}

		public async Task<string> ObtainTicket()
		{
			var entity = new EntityKey()
			{
				Id = _userContext.EntityId,
				Type = _userContext.EntityType
			};
			var result = await _userApi.CreateMatchmakingTicketAsync(new CreateMatchmakingTicketRequest()
			{
				GiveUpAfterSeconds = 1000,
				QueueName = _config.QueueName,
				Creator = new MatchmakingPlayer()
				{
					Entity = entity,
				},
			});
			_errorHandler.CheckErrors(result);
			return result.Result.TicketId;
		}
	}
}