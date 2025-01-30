using System;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using Microsoft.AspNetCore.Mvc;
using Quantum;
using ServerCommon.Authentication.ApiKey;
using ServerCommon.Cloudscript.Models;

namespace ServerCommon.Cloudscript
{
	[ApiController]
	[Route("currency")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class EconomyController : ControllerBase
	{
		private readonly IServerStateService _serverState;
		private readonly IUserMutex _userMutex;

		public EconomyController(IServerStateService state, IUserMutex userMutex)
		{
			_serverState = state;
			_userMutex = userMutex;
		}

		[HttpGet]
		[RequiresApiKey]
		[Route("getcurrency")]
		public async Task<dynamic> GetCurrency(string playerId, int currencyId)
		{
			var state = await _serverState.GetPlayerState(playerId);
			var playerData = state.DeserializeModel<PlayerData>();
			var currencyGameId = (GameId) currencyId;
			playerData.Currencies.TryGetValue(currencyGameId, out var currencyAmount);
			return Ok(currencyAmount);
		}

		[HttpPost]
		[RequiresApiKey]
		[Route("modifycurrency")]
		public async Task<dynamic> ModifyCurrency([FromBody] CurrencyUpdateRequest request)
		{
			await using (await _userMutex.LockUser(request.PlayerId))
			{
				var state = await _serverState.GetPlayerState(request.PlayerId);
				var playerData = state.DeserializeModel<PlayerData>();
				var currencyGameId = (GameId) request.CurrencyId;
				playerData.Currencies.TryGetValue(currencyGameId, out var currencyAmount);
				var castedAmount = Convert.ToInt64(currencyAmount);
				castedAmount += request.Delta;
				if (castedAmount < 0)
				{
					return Conflict(
						$"{castedAmount} of currency {currencyGameId}, not enough for delta {request.Delta}");
				}

				playerData.Currencies[currencyGameId] = Convert.ToUInt64(castedAmount);
				state.UpdateModel(playerData);
				await _serverState.UpdatePlayerState(request.PlayerId, state);
				return Ok(castedAmount);
			}
		}
	}
}