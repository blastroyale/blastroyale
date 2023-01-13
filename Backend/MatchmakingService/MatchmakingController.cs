using System.Collections.Generic;
using System.Threading.Tasks;
using ServerCommon.Cloudscript;
using Microsoft.AspNetCore.Mvc;
using ServerCommon.Authentication.ApiKey;

namespace Firstlight.Matchmaking
{
	[ApiController]
	[Route("matchmaking")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class MatchmakingController : ControllerBase
	{
		private readonly IMatchmakingServer _server;

		public MatchmakingController(IMatchmakingServer server)
		{
			_server = server;
		}

		[HttpPost]
		[RequiresApiKey]
		[Route("LeaveMatchmaking")]
		public async Task<IActionResult> LeaveMatchmaking([FromBody] CloudscriptRequest request)
		{
			return Ok(await _server.LeaveMatchmaking(request));
		}

		[HttpPost]
		[RequiresApiKey]
		[Route("StartMatchmaking")]
		public async Task<IActionResult> StartMatchmaking([FromBody] CloudscriptRequest request)
		{
			return Ok(await _server.StartMatchmaking(request));
		}

		[HttpPost]
		[RequiresApiKey]
		[Route("GetTicket")]
		public async Task<IActionResult> GetTicket([FromBody] CloudscriptRequest request)
		{
			return Ok(await _server.GetTicket(request));
		}

		[HttpPost]
		[RequiresApiKey]
		[Route("GetTickets")]
		public async Task<IActionResult> GetTickets([FromBody] CloudscriptRequest request)
		{
			return Ok(await _server.GetTickets(request));
		}
	}
}