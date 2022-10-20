using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContainerApp.Cloudscript;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
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
		private IServerStateService _state;
		private IErrorService<PlayFabError> _errorHandler;
		private IServerMutex _mutex;

		public ShopService(ILogger log, IServerStateService state, IErrorService<PlayFabError> errorHandler, IServerMutex mutex)
		{
			_log = log;
			_state = state;
			_errorHandler = errorHandler;
			_mutex = mutex;
		}

		/// <summary>
		/// Proccess a given purchase.
		/// Will search in players inventory for an item that references the given catalog item id.
		/// If it finds, will consume the item and award its configured RewardData
		/// </summary>
		public async Task<PlayFabResult<BackendLogicResult>> ProcessPurchaseRequest(string playerId, string catalogItemId)
		{
			_log.Log(LogLevel.Information, $"{playerId} is executing - ConsumeValidatedPurchaseCommand");

			var item = await FindCatalogItem(catalogItemId);
			var rewardData = JsonConvert.DeserializeObject<RewardData>(item.CustomData);
				
			_log.Log(LogLevel.Information, $"Consuming the Purchase for  {playerId} - " +
										   $"item id: {catalogItemId} - rewarding: {item.CustomData}");
			
			await ConvertInventoryItemToUserReadonlyDataItem(playerId, rewardData);
			await ConsumeItem(playerId, item);
			var result = new PlayFabResult<BackendLogicResult>
			{
				Result = new BackendLogicResult
				{
					PlayFabId = playerId,
					Data = new Dictionary<string, string>()
				}
			};
			ModelSerializer.SerializeToData(result.Result.Data, rewardData);
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

		private async Task ConsumeItem(string playerId, CatalogItem item)
		{
			var inventoryRequest = new GetUserInventoryRequest { PlayFabId = playerId };
			var consumeRequest = new ConsumeItemRequest { ConsumeCount = 1, PlayFabId = playerId };
			var inventoryResult = await PlayFabServerAPI.GetUserInventoryAsync(inventoryRequest);
			_errorHandler.CheckErrors(inventoryResult);
			var itemInstance = inventoryResult.Result.Inventory.FirstOrDefault(i => i.ItemId == item.ItemId);
			if (itemInstance == null)
			{
				throw new LogicException($"ConsumeValidatedPurchaseCommand error: - " +
										 $"Null item {item.ItemId}");
			}
			consumeRequest.ItemInstanceId = itemInstance.ItemInstanceId;
			var consumeResult = await PlayFabServerAPI.ConsumeItemAsync(consumeRequest);
			_errorHandler.CheckErrors(consumeResult);
		}

		/// <summary>
		/// Function responsible for converting an item added to the user inventory to user readonly data.
		/// THe data provided to this function is the model that the game uses as the custom json data in catalog
		/// items.
		/// </summary>
		private async Task ConvertInventoryItemToUserReadonlyDataItem(string playerId, RewardData rewardData)
		{
			try
			{
				_mutex.Lock(playerId);
				var serverState = await _state.GetPlayerState(playerId);
				var playerData = serverState.DeserializeModel<PlayerData>();
				playerData.UncollectedRewards.Add(rewardData);
				serverState.UpdateModel(playerData);
				await _state.UpdatePlayerState(playerId, serverState);
			}
			finally
			{
				_mutex.Unlock(playerId);
			}
		
		}
	}
}