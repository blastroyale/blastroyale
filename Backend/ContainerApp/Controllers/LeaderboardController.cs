using System.Threading.Tasks;
using Backend.Game.Services;
using Microsoft.AspNetCore.Mvc;
using PlayFab;
using PlayFab.ServerModels;
using ServerCommon;


namespace ContainerApp.Controllers
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
		public async Task<dynamic> GetLeaderboard(string name, int position)
		{
			var result = await PlayFabServerAPI.GetLeaderboardAsync(new GetLeaderboardRequest()
			{
				StatisticName = name,
				StartPosition = position,
				MaxResultsCount = 30
			});
			_errorHandler.CheckErrors(result);
			return Ok(result.Result.Leaderboard);
		}
	}
}