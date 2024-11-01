using System;
using System.Threading.Tasks;
using Backend.Game.Services;
using Microsoft.AspNetCore.Mvc;
using PlayFab;
using PlayFab.ServerModels;


namespace ServerCommon.Controllers
{
	[ApiController]
	[Route("leaderboard")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class LeaderboardController : ControllerBase
	{
		private IGameConfigurationService _configs;
		private IErrorService<PlayFabError> _errorHandler;

		public LeaderboardController(IGameConfigurationService cfg, IErrorService<PlayFabError> errors)
		{
			_configs = cfg;
			_errorHandler = errors;
		}

		[HttpGet]
		[Route("get")]
		public async Task<dynamic> GetLeaderboard(string name, int position, int limit)
		{
			var result = await PlayFabServerAPI.GetLeaderboardAsync(new GetLeaderboardRequest()
			{
				StatisticName = name,
				StartPosition = position,
				ProfileConstraints = new ()
				{
					ShowDisplayName = true,
					ShowAvatarUrl = true
				},
				MaxResultsCount = Math.Min(200, limit)
			});
			_errorHandler.CheckErrors(result);
			return Ok(result.Result.Leaderboard);
		}

		[HttpGet]
		[Route("getrank")]
		public async Task<dynamic> GetPlayerRank(string name, string playerId)
		{
			var result = await PlayFabServerAPI.GetLeaderboardAroundUserAsync(new()
			{
				StatisticName = name,
				PlayFabId = playerId,
				MaxResultsCount = 1
			});
			_errorHandler.CheckErrors(result);
			return Ok(result.Result.Leaderboard);
		}
	}
}