using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServerCommon.Cloudscript;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using GameLogicService.Game;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using ServerCommon;

namespace GameLogicService.Services
{
	/// <summary>
	/// Code copied and adapted from previous function code:
	/// https://bitbucket.org/first-light/blast-lands/src/develop/Backend/Backend/Functions/ConsumeValidatedPurchaseCommand.cs
	///
	/// The flow is:
	/// - User buys item in Unity IAP
	/// - Playfab Validates it, sends response to client.
	/// - Playfab creates a Inventory Item referencing this Catalog Item player bought
	/// - Catalog Items have Custom Data that represents game RewardData class
	/// - User calls this service to consume this InventoryItem, awarding the RewardData defined in the Catalog Item
	/// - Reward Data is added to user Unclaimed Rewards
	/// - Added reward data is returned in the response so client adds on its side too
	/// </summary>
	public class ShopService
	{
		private ILogger _log;
		private IErrorService<PlayFabError> _errorHandler;
		private IBaseServiceConfiguration _cfg;
		private IEventManager _events;

		public ShopService(ILogger log, IErrorService<PlayFabError> errorHandler, IBaseServiceConfiguration cfg, IEventManager events)
		{
			_log = log;
			_errorHandler = errorHandler;
			_cfg = cfg;
			_events = events;
		}

		/// <summary>
		/// Proccess a given purchase.
		/// Will search in players inventory for an item that references the given catalog item id.
		/// If it finds, will consume the item and award its configured RewardData
		/// </summary>
		public async Task<PlayFabResult<BackendLogicResult>> ProcessPurchaseRequest(
			string playerId, string catalogItemId, bool fakeStore)
		{
			_log.Log(LogLevel.Information, $"{playerId} is executing - ConsumeValidatedPurchaseCommand");

			var item = await FindCatalogItem(catalogItemId);
			
			if (fakeStore && _cfg.DevelopmentMode)
			{
				var res = await PlayFabServerAPI.GrantItemsToUserAsync(new()
				{
					CatalogVersion = "Store",
					ItemIds = new () {catalogItemId},
					PlayFabId = playerId
				});
				if (res.Error != null) throw new Exception(res.Error.GenerateErrorReport());
				_log.LogInformation($"Given store test free item {catalogItemId} to player {playerId}");
			}
			
			await _events.CallEvent(new IAPPurchasedEvent(playerId));
			await _events.CallEvent(new InventoryUpdatedEvent(playerId));

			var result = Playfab.Result(playerId);
			ModelSerializer.SerializeToData(result.Result.Data, JsonConvert.DeserializeObject<LegacyItemData>(item.CustomData));
			return result;
		}
	
		private async Task<CatalogItem> FindCatalogItem(string item)
		{
			var request = new GetCatalogItemsRequest { CatalogVersion = "Store" };
			var catalogResult = await PlayFabServerAPI.GetCatalogItemsAsync(request);
			_errorHandler.CheckErrors(catalogResult);
			var catalogItem = catalogResult.Result.Catalog.FirstOrDefault(i => i.ItemId == item);
			if (catalogItem != null)
			{
				return catalogItem;
			}
			throw new LogicException($"no catalog item with the given item id: {item}");
		}
	}
}