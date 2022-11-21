using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GameLogicApp
{
	[ApiController]
	[Route("app")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class AppController : ControllerBase
	{
		[Route("health")]
		[HttpGet]
		public async Task<IActionResult> HealthCheck()
		{
			return Ok();
		}
	}
}

