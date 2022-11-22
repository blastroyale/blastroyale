using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Backend;
using Backend.Game.Services;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Services;
using GameLogicApp.Cloudscript.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using Quantum;
using ServerShared.Authentication.ApiKey;

namespace GameLogicApp.Cloudscript
{
	[ApiController]
	[Route("currency")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class EconomyController : ControllerBase
	{
		private readonly IServerStateService _serverState;
		private readonly IServerMutex _mutex;

		public EconomyController(IServerStateService state, IServerMutex mutex)
		{
			_serverState = state;
			_mutex = mutex;
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
			try
			{
				await _mutex.Lock(request.PlayerId);
				var state = await _serverState.GetPlayerState(request.PlayerId);
				var playerData = state.DeserializeModel<PlayerData>();
				var currencyGameId = (GameId) request.CurrencyId;
				playerData.Currencies.TryGetValue(currencyGameId, out var currencyAmount);
				var castedAmount = Convert.ToInt64(currencyAmount);
				castedAmount += request.Delta;
				if (castedAmount < 0)
				{
					return Conflict($"{castedAmount} of currency {currencyGameId}, not enough for delta {request.Delta}");
				} 
				playerData.Currencies[currencyGameId] = Convert.ToUInt64(castedAmount);
				state.UpdateModel(playerData);
				_serverState.UpdatePlayerState(request.PlayerId, state);
				return Ok(castedAmount);
			}
			finally
			{
				_mutex.Unlock(request.PlayerId);
			}
		}
	}
}