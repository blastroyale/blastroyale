using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using GameLogicService.Models;
using GameLogicService.Services;
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
		private IUserMutex _userMutex;
		private IServerStateService _state;
		private IServerAnalytics _analytics;
		private IRemoteConfigService _remoteConfig;

		public EquipmentController(ILogger log, IServerAnalytics analytics, IServerStateService state,
								   IUserMutex userMutex, IGameLogicContextService logicContext,
								   IGameConfigurationService cfg, IRemoteConfigService remoteConfig)
		{
			_log = log;
			_configs = cfg;
			_remoteConfig = remoteConfig;
			_logicContext = logicContext;
			_userMutex = userMutex;
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
			var (state, remoteConfigs) = await _state.FetchStateAndConfigs(_remoteConfig, playerId, 0);
			var ctx = await _logicContext.GetLogicContext(playerId, remoteConfigs, state);
			var inv = ctx.GameLogic.EquipmentLogic.GetInventoryEquipmentInfo(EquipmentFilter.All);
			return Content(JsonConvert.SerializeObject(
				inv.Select(i => new
				{
					UniqueId = i.Id,
					i.Equipment,
					TokenId = ctx.GameLogic.EquipmentLogic.NftInventory.TryGetValue(i.Id, out var nft)
						? nft.TokenId
						: null,
					Hash = i.Equipment.GetServerHashCode()
				})), "application/json");
		}

		[HttpPost]
		[RequiresApiKey]
		[Route("RemoveEquipment")]
		public async Task<dynamic> RemoveEquipment(string playerId, uint uniqueId, int hash)
		{
			await using (await _userMutex.LockUser(playerId))
			{
				try
				{
					var id = new UniqueId(uniqueId);
					var (playerState, remoteConfigs) = await _state.FetchStateAndConfigs(_remoteConfig, playerId, 0);

					var ctx = await _logicContext.GetLogicContext(playerId, remoteConfigs, playerState);
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
				await using (await _userMutex.LockUser(playerId))
				{
					var (state, remoteConfigs) = await _state.FetchStateAndConfigs(_remoteConfig, playerId, 0);
					var ctx = await _logicContext.GetLogicContext(playerId, remoteConfigs, state);
					var uniqueId = ctx.GameLogic.EquipmentLogic.AddToInventory(request.Equipment);
					var updatedState = ctx.PlayerData.GetUpdatedState();
					if (!string.IsNullOrEmpty(request.TokenId))
					{
						var data = updatedState.DeserializeModel<EquipmentData>();
						data.NftInventory[uniqueId] = new NftEquipmentData()
						{
							InsertionTimestamp = DateTime.UtcNow.Ticks,
							TokenId = request.TokenId
						};
						updatedState.UpdateModel(data);
					}

					await _state.UpdatePlayerState(playerId, updatedState);
					var analytics = request.Equipment.GetAnalyticsData();
					analytics["user"] = request.PlayerId;
					_analytics.EmitEvent("API Item Added", analytics);
					return Ok(uniqueId);
				}
			}
			catch (Exception e)
			{
				_log.LogError(e, "Error Removing Item from Player");
				return Problem(e.Message);
			}
		}
	}
}