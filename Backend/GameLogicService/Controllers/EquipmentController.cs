using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Game.Utils;
using Microsoft.AspNetCore.Mvc;
using Quantum;
using ServerCommon.Authentication.ApiKey;


namespace ServerCommon.Cloudscript
{
	[ApiController]
	[Route("equipment")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class EquipmentController : ControllerBase
	{
		private IGameConfigurationService _configs;

		public EquipmentController(IGameConfigurationService cfg)
		{
			_configs = cfg;
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("getstats")]
		public async Task<dynamic> GetStats([FromBody] Equipment request)
		{
			var gameConfiguration = await _configs.GetGameConfigs();
			var stats = request.GetStats(gameConfiguration);
			return Ok(stats);
		}
	}
}