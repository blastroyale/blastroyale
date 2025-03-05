using System;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using GameLogicService.Game;
using Microsoft.Extensions.Logging;
using PlayFab;
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

		private IUserMutex _userMutex;
		private IInventorySyncService<ItemData> _inventorySync;
		private readonly IServerStateService _serverState;

		private IErrorService<PlayFabError> _errorHandler;
		private IBaseServiceConfiguration _cfg;
		private IEventManager _events;
		private IItemCatalog<ItemData> _catalog;
		private IStoreService _storeService;

		public ShopService(ILogger log, IItemCatalog<ItemData> catalog, IErrorService<PlayFabError> errorHandler,
						   IBaseServiceConfiguration cfg, IEventManager events, IUserMutex userMutex,
						   IServerStateService serverState, IInventorySyncService<ItemData> inventorySync, IStoreService storeService)
		{
			_log = log;
			_errorHandler = errorHandler;
			_cfg = cfg;
			_events = events;
			_userMutex = userMutex;
			_serverState = serverState;
			_inventorySync = inventorySync;
			_catalog = catalog;
			_storeService = storeService;
		}

		/// <summary>
		/// Proccess a given purchase.
		/// Will search in players inventory for an item that references the given catalog item id.
		/// If it finds, will consume the item and award its configured RewardData
		/// </summary>
		public async Task<PlayFabResult<BackendLogicResult>> ProcessPurchaseRequest(
			string playerId, string catalogItemId)
		{
			_log.Log(LogLevel.Information, $"{playerId} is executing - ConsumeValidatedPurchaseCommand");

			var item = await _catalog.GetCatalogItem(catalogItemId);
			if (_cfg.DevelopmentMode)
			{
				var res = await PlayFabServerAPI.GrantItemsToUserAsync(new()
				{
					CatalogVersion = "Store",
					ItemIds = new() { catalogItemId },
					PlayFabId = playerId
				});
				if (res.Error != null) throw new Exception(res.Error.GenerateErrorReport());
				_log.LogInformation($"Given store test free item {catalogItemId} to player {playerId}");
			}

			await _events.CallEvent(new IAPPurchasedEvent(playerId));
			await SyncPurchaseItems(playerId, item, catalogItemId);

			var result = Playfab.Result(playerId);
			ModelSerializer.SerializeToData(result.Result.Data, item);
			return result;
		}

		public async Task SyncPurchaseItems(string playerId, ItemData purchasedItem, string catalogItemId)
		{
			await using (await _userMutex.LockUser(playerId))
			{
				var state = await _serverState.GetPlayerState(playerId);
				var givenItems = await _inventorySync!.SyncData(state, playerId);
				if (state.HasDelta())
				{
					await _serverState.UpdatePlayerState(playerId, state.GetOnlyUpdatedState());
					if (givenItems.Contains(purchasedItem))
					{
						var contentCreatorData = state.DeserializeModel<ContentCreatorData>();

						var purchasedItemPrice = await GetItemPrice(state, catalogItemId);
						var (currencyId, itemValue) = purchasedItemPrice.Price.FirstOrDefault(); 
						
						var msg = new PurchaseClaimedMessage
						{
							ItemPurchased = purchasedItem,
							SupportingContentCreator = contentCreatorData.SupportingCreatorCode,
							PriceCurrencyId = currencyId,
							PricePaid = itemValue.ToString()
							
						};
						await _events.CallEvent(new GameLogicMessageEvent<PurchaseClaimedMessage>(playerId, msg));
					}
				}
			}
		}

		private async Task<FlgStoreItem> GetItemPrice(ServerState state, string catalogItemId)
		{
			var dailyDealsConfiguration = state.DeserializeModel<PlayerStoreData>().PlayerDailyDealsConfiguration;

			//Check if purchased Item is part of Player's Daily Deal Items
			if (dailyDealsConfiguration != null)
			{
				var specialStore = dailyDealsConfiguration.SpecialStoreList.FirstOrDefault(s => s.SpecialStoreItemIDs.Contains(catalogItemId));
				
				return await _storeService.GetItemPrice(catalogItemId, specialStore.SpecialStoreName);
			}
			return await _storeService.GetItemPrice(catalogItemId);
		}
	}
}