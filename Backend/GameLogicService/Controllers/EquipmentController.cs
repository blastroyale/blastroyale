using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using GameLogicService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
		private IGameLogicContextService _logicContext;
		private ILogger _log;
		private IServerMutex _mutex;
		private IServerStateService _state;
		private IServerAnalytics _analytics;

		public EquipmentController(ILogger log, IServerAnalytics analytics, IServerStateService state, IServerMutex mutex, IGameLogicContextService logicContext, IGameConfigurationService cfg)
		{
			_log = log;
			_configs = cfg;
			_logicContext = logicContext;
			_mutex = mutex;
			_state = state;
			_analytics = analytics;
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
		
		[HttpGet]
		[RequiresApiKey]
		[Route("GetEquipment")]
		public async Task<dynamic> GetEquipment(string playerId)
		{
			var ctx = await _logicContext.GetLogicContext(playerId);
			var inv = ctx.GameLogic.EquipmentLogic.GetInventoryEquipmentInfo(EquipmentFilter.All);
			return Content(JsonConvert.SerializeObject(
				inv.Select(i => new
				{
					UniqueId = i.Id,
					i.Equipment,
					Hash = i.Equipment.GetServerHashCode()
				})), "application/json");
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("RemoveEquipment")]
		public async Task<dynamic> RemoveEquipment(string playerId, uint uniqueId, int hash)
		{
			try
			{
				await _mutex.Lock(playerId);
				var id = new UniqueId(uniqueId);
				var playerState = await _state.GetPlayerState(playerId);
				var ctx = await _logicContext.GetLogicContext(playerId, playerState);
				if (!ctx.GameLogic.EquipmentLogic.Inventory.TryGetValue(id, out var equipment))
					return Conflict($"Equipment {uniqueId} not found");
				if (equipment.GetServerHashCode() != hash)
					return Conflict($"Hash not matching");
				ctx.GameLogic.EquipmentLogic.RemoveFromInventory(id);
				var updatedState = playerState.GetOnlyUpdatedState();
				await _state.UpdatePlayerState(playerId, updatedState);
				var analytics = equipment.GetAnalyticsData();
				analytics["user"] = playerId;
				_analytics.EmitEvent("API Item Removed", analytics);
			}
			catch (Exception e)
			{
				_log.LogError(e, "Error Removing Item from Player");
				return Problem(e.Message);
			}
			finally
			{
				_mutex.Unlock(playerId);
			}
			return Ok();
		}
		
		[HttpPost]
		[RequiresApiKey]
		[Route("AddEquipment")]
		public async Task<dynamic> AddEquipment([FromBody] AddEquipmentRequest request)
		{
			try
			{
				var playerId = request.PlayerId;
				await _mutex.Lock(playerId);
				var playerState = await _state.GetPlayerState(playerId);
				var ctx = await _logicContext.GetLogicContext(playerId, playerState);
				var uniqueId = ctx.GameLogic.EquipmentLogic.AddToInventory(request.Equipment);
				var updatedState = playerState.GetOnlyUpdatedState();
				await _state.UpdatePlayerState(playerId, updatedState);
				var analytics = request.Equipment.GetAnalyticsData();
				analytics["user"] = request.PlayerId;
				_analytics.EmitEvent("API Item Added", analytics);
				return Ok(uniqueId);
			}
			catch (Exception e)
			{
				_log.LogError(e, "Error Removing Item from Player");
				return Problem(e.Message);
			}
			finally
			{
				_mutex.Unlock(request.PlayerId);
			}
		}
	}
}