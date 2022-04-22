using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Backend.Context;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;

namespace Backend.Functions
{
	/// <summary>
	/// This command is executed by validate the player's receipt and award the items to the player
	/// </summary>
	public static class ConsumeValidatedPurchaseCommand
	{
		/// <summary>
		/// Command Execution
		/// </summary>
		[FunctionName("ConsumeValidatedPurchaseCommand")]
		public static async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		                                           HttpRequestMessage req, ILogger log)
		{
			var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
			var server = new PlayFabServerInstanceAPI(context.ApiSettings, context.AuthenticationContext);
			var itemId = context.FunctionArgument.Data["item_id"];
			var result = new PlayFabResult<BackendLogicResult>
			{
				Result = new BackendLogicResult
				{
					PlayFabId = context.AuthenticationContext.PlayFabId,
					Data = new Dictionary<string, string>()
				}
			};
			
			log.Log(LogLevel.Information, $"{context.AuthenticationContext.PlayFabId} is executing - ConsumeValidatedPurchaseCommand");

			var item = await GetPurchaseItem(server, result, context.FunctionArgument.Data["item_id"]);
			var itemData = JsonConvert.DeserializeObject<CatalogItemCustomData>(item.CustomData);
				
			log.Log(LogLevel.Information, $"Consuming the Purchase for: {context.AuthenticationContext.PlayFabId} - " +
			                              $"item id: {itemId} - rewarding: {item.CustomData}");


			await ConsumeItem(server, item);
			await UpdateUserData(server, itemData);
			
			result.Result.Data.Add(item.ItemId, JsonConvert.SerializeObject(item));

			return result;
		}

		private static async Task<CatalogItem> GetPurchaseItem(PlayFabServerInstanceAPI server, 
		                                                       PlayFabResult<BackendLogicResult> result,
		                                                       string item)
		{
			var request = new GetCatalogItemsRequest { CatalogVersion = "Store" };
			var catalogResult = await server.GetCatalogItemsAsync(request);

			result.CustomData = catalogResult.CustomData;
			result.Error = catalogResult.Error;

			if (result.Error != null)
			{
				throw new LogicException($"ConsumeValidatedPurchaseCommand error: {server.authenticationContext.PlayFabId} - " +
				                         $"while getting the catalog item with the given idem id: {item} - " +
				                         $"{result.Error.GenerateErrorReport()}");
			}

			foreach (var catalogItem in catalogResult.Result.Catalog)
			{
				if (item == catalogItem.ItemId)
				{
					return catalogItem;
				}
			}

			throw new LogicException($"ConsumeValidatedPurchaseCommand error: {server.authenticationContext.PlayFabId} - " +
			                         $"no catalog item with the given item id: {item}");
		}

		private static async Task ConsumeItem(PlayFabServerInstanceAPI server, CatalogItem item)
		{
			var inventoryRequest = new GetUserInventoryRequest { PlayFabId = server.authenticationContext.PlayFabId };
			var consumeRequest = new ConsumeItemRequest { ConsumeCount = 1, PlayFabId = server.authenticationContext.PlayFabId };
			var inventoryResult = await server.GetUserInventoryAsync(inventoryRequest);
			
			if (inventoryResult.Error != null)
			{
				throw new LogicException($"ConsumeValidatedPurchaseCommand error: {server.authenticationContext.PlayFabId} - " +
				                         $"while requesting the user inventory - {inventoryResult.Error.GenerateErrorReport()}");
			}

			foreach (var inventoryItem in inventoryResult.Result.Inventory)
			{
				if (inventoryItem.ItemId == item.ItemId)
				{
					consumeRequest.ItemInstanceId = inventoryItem.ItemInstanceId;
					break;
				}
			}
			
			var consumeResult = await server.ConsumeItemAsync(consumeRequest);

			if (consumeResult.Error != null)
			{
				throw new LogicException($"ConsumeValidatedPurchaseCommand error: {server.authenticationContext.PlayFabId} - " +
				                         $"while consuming the item {item.ItemId} - {consumeResult.Error.GenerateErrorReport()}");
			}
		}

		private static async Task UpdateUserData(PlayFabServerInstanceAPI server, CatalogItemCustomData itemData)
		{
			var dataRequest = new GetUserDataRequest
			{
				Keys = new List<string> { nameof(PlayerData) },
				PlayFabId = server.authenticationContext.PlayFabId
			};

			var dataResult = await server.GetUserReadOnlyDataAsync(dataRequest);

			if (dataResult.Error != null)
			{
				throw new LogicException($"ConsumeValidatedPurchaseCommand error: {server.authenticationContext.PlayFabId} - " +
				                         $"while requesting the user data - {dataResult.Error.GenerateErrorReport()}");
			}
			
			var playerData = JsonConvert.DeserializeObject<PlayerData>(dataResult.Result.Data[nameof(PlayerData)].Value);

			playerData.Currencies[itemData.RewardGameId] += itemData.RewardValue;
			
			var updateRequest = new UpdateUserDataRequest
			{
				PlayFabId = server.authenticationContext.PlayFabId,
				Data =  new Dictionary<string, string>
				{
					{ nameof(PlayerData), JsonConvert.SerializeObject(playerData) },
				} 
			};
				
			var updateResult = await server.UpdateUserReadOnlyDataAsync(updateRequest);

			if (updateResult.Error != null)
			{
				throw new LogicException($"ConsumeValidatedPurchaseCommand error: {server.authenticationContext.PlayFabId} - " +
				                         $"while updating the user data - {updateResult.Error.GenerateErrorReport()}");
			}
		}
	}
}