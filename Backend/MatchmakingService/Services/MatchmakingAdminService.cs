using System.Net.Http;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.AuthenticationModels;
using PlayFab.MultiplayerModels;
using ServerCommon;
using EntityKey = PlayFab.MultiplayerModels.EntityKey;

namespace Firstlight.Matchmaking
{
	/// <summary>
	/// Playfab admin services to control matchmaking.
	/// </summary>
	public interface IMatchmakingAdminService
	{
		/// <summary>
		/// Obtains match details by a given match id
		/// </summary>
		public Task<GetMatchResult> GetMatch(string matchId);
	}

	public class PlayfabMatchmakingAdminService : IMatchmakingAdminService
	{
		private readonly MatchmakingConfig _config;
		private PlayFabAuthenticationContext _titleContext;
		private IErrorService<PlayFabError> _errorHandler;

		public PlayfabMatchmakingAdminService(MatchmakingConfig config, IErrorService<PlayFabError> errorHandler)
		{
			_errorHandler = errorHandler;
			_config = config;
		}

		public async Task<GetMatchResult> GetMatch(string matchId)
		{
			var result = await PlayFabMultiplayerAPI.GetMatchAsync(new GetMatchRequest()
			{
				QueueName = _config.QueueName,
				MatchId = matchId,
				AuthenticationContext =
					await GetTitleContext()
			});
			_errorHandler.CheckErrors(result);
			return result.Result;
		}

		public async Task<PlayFabAuthenticationContext> GetTitleContext()
		{
			if (_titleContext != null)
			{
				return _titleContext;
			}

			var result = await PlayFabAuthenticationAPI.GetEntityTokenAsync(new GetEntityTokenRequest());
			_errorHandler.CheckErrors(result);
			_titleContext = new PlayFabAuthenticationContext()
			{
				EntityId = PlayFabSettings.staticSettings.TitleId,
				EntityToken = result.Result.EntityToken,
				EntityType = "title",
			};
			return _titleContext;
		}
	}
}