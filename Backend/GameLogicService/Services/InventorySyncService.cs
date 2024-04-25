using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Models;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLightServerSDK.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using Quantum;
using ItemInstance = PlayFab.ServerModels.ItemInstance;


namespace GameLogicService.Services
{
	/// <summary>
	/// Syncs playfab inventory items with user data
	/// It will remove any pending items and currency from playfab inventory
	/// and convert to our currency datas
	/// </summary>
	public class PlayfabInventorySyncService : IInventorySyncService
	{
		private PluginContext _ctx;
		private ILogger _log;
		private IItemCatalog<ItemData> _catalog;

		public PlayfabInventorySyncService(ILogger log, IItemCatalog<ItemData> catalog)
		{
			_log = log;
			_catalog = catalog;
		}

		private async Task<int> SyncCurrency(string player, GetUserInventoryResult inventory, PlayerData playerData, GameId gameId)
		{
			var playfabName = PlayfabCurrencies.GetPlayfabCurrencyName(gameId);
			inventory.VirtualCurrency.TryGetValue(playfabName, out var playfabAmount);
			if (playfabAmount > 0)
			{
				playerData.UncollectedRewards.Add(ItemFactory.Currency(gameId, playfabAmount));
				var res = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(new()
				{
					Amount = playfabAmount,
					PlayFabId = player,
					VirtualCurrency = playfabName
				});
				if (res.Error != null) throw new Exception(res.Error.GenerateErrorReport());
				_log.LogInformation($"[Playfab Sync] Synced {playfabAmount} x {gameId} for user {player}");
				return playfabAmount;
			}

			return 0;
		}

		public async Task<bool> SyncData(ServerState state, string player)
		{
			var consumedItems = new List<ItemInstance>();
			var consumedCurrencies = new Dictionary<GameId, int>();
			try
			{
				var result = await PlayFabServerAPI.GetUserInventoryAsync(new()
				{
					PlayFabId = player,
				});
				if (result.Error != null) throw new Exception(result.Error.GenerateErrorReport());
				var inventory = result.Result;

				var playerData = state.DeserializeModel<PlayerData>();

				var currencies = new[] { GameId.COIN, GameId.CS, GameId.BlastBuck, GameId.NOOB };
				foreach (var gameId in currencies)
				{
					consumedCurrencies[gameId] = await SyncCurrency(player, inventory, playerData, gameId);
				}

				if (inventory.Inventory.Count > 0)
				{
					foreach (var item in inventory.Inventory)
					{
						var itemData = await _catalog.GetCatalogItem(item.ItemId);
						playerData.UncollectedRewards.Add(itemData);
						var res = await PlayFabServerAPI.ConsumeItemAsync(new ConsumeItemRequest
							{ ConsumeCount = 1, PlayFabId = player, ItemInstanceId = item.ItemInstanceId });
						if (res.Error != null) throw new Exception(res.Error.GenerateErrorReport());
						consumedItems.Add(item);
						_log.LogInformation($"[Playfab Sync] Synced item {item.DisplayName} -> {itemData.Id} to player {player}");
					}
				}

				state.UpdateModel(playerData);
			}
			catch (Exception e)
			{
				_log.LogError(e.Message + e.StackTrace);
				_log.LogError("[Playfab Sync] Exception when syncing inventory, rolling back items removed");
				var itemIds = consumedItems.Select(item => item.ItemId);
				var list = itemIds.ToList();
				if (itemIds.Any())
				{
					var res = await PlayFabServerAPI.GrantItemsToUserAsync(new()
					{
						ItemIds = list,
						PlayFabId = player
					});
					if (res.Error != null)
					{
						var itemsString = string.Join(",", itemIds);
						_log.LogError($"CRITICAL ON ITEM ROLLBACK: Items {itemsString} to player {player}: {res.Error.GenerateErrorReport()}");
						_ctx.Analytics.EmitEvent("Item Vanished", new AnalyticsData()
						{
							{ "items", itemsString }, { "affectedPlayer", player }
						});
					}
				}

				foreach (var (currency, amt) in consumedCurrencies)
				{
					if (amt == 0) continue;
					var res2 = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new()
					{
						Amount = amt,
						PlayFabId = player,
						VirtualCurrency = PlayfabCurrencies.GetPlayfabCurrencyName(currency)
					});
					if (res2.Error != null)
					{
						_log.LogError($"CRITICAL ON CURRENCY ROLLBACK: Currency {amt} x {currency} to player {player}: {res2.Error.GenerateErrorReport()}");
						_ctx.Analytics.EmitEvent("Item Vanished", new AnalyticsData()
						{
							{ "currency", currency }, { "amount", amt }, { "affectedPlayer", player }
						});
					}
				}

				throw;
			}

			return true;
		}
	}
}