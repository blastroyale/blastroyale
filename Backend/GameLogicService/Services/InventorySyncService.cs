using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Game.Services;
using Backend.Plugins;
using BlastRoyaleNFTPlugin;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services.Analytics;
using FirstLight.Models;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLightServerSDK.Services;
using Microsoft.Extensions.Logging;
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
	public class PlayfabInventorySyncService : IInventorySyncService<ItemData>
	{
		private IServerAnalytics _analytics;
		private ILogger _log;
		private IItemCatalog<ItemData> _catalog;
		
		public PlayfabInventorySyncService(IServerAnalytics analytics, ILogger log, IItemCatalog<ItemData> catalog)
		{
			_log = log;
			_catalog = catalog;
			_analytics = analytics;
		}

		private async Task<int> SyncCurrency(string player, GetUserInventoryResult inventory, PlayerData playerData,
											 ServerState state, GameId gameId)
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

		public async Task<IReadOnlyList<ItemData>> SyncData(ServerState state, string player)
		{
			var consumedItems = new List<ItemInstance>();
			var givenGameItems = new List<ItemData>();
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
					consumedCurrencies[gameId] = await SyncCurrency(player, inventory, playerData, state, gameId);
					givenGameItems.Add(ItemFactory.Currency(gameId, consumedCurrencies[gameId]));
				}

				if (inventory.Inventory.Count > 0)
				{
					foreach (var item in inventory.Inventory)
					{
						var res = await PlayFabServerAPI.ConsumeItemAsync(new ConsumeItemRequest
							{ ConsumeCount = 1, PlayFabId = player, ItemInstanceId = item.ItemInstanceId });
						if (res.Error != null) throw new Exception(res.Error.GenerateErrorReport());
						
						var itemData = await _catalog.GetCatalogItem(item.ItemId);
						playerData.UncollectedRewards.Add(itemData);
						consumedItems.Add(item);
						givenGameItems.Add(itemData);
						_log.LogInformation(
							$"[Playfab Sync] Synced item {item.DisplayName} -> {itemData.Id} to player {player}");
					}
					
					//If any item synced is a Bundle, there's no need to keep track on it inside UnclaimedRewards
					if (playerData.UncollectedRewards.FirstOrDefault(ur => ur.Id == GameId.Bundle) != null)
					{
						playerData.UncollectedRewards.Remove(playerData.UncollectedRewards.FirstOrDefault(ur => ur.Id == GameId.Bundle));
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
						_log.LogError(
							$"CRITICAL ON ITEM ROLLBACK: Items {itemsString} to player {player}: {res.Error.GenerateErrorReport()}");
						_analytics.EmitEvent("Item Vanished", new AnalyticsData()
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
						_log.LogError(
							$"CRITICAL ON CURRENCY ROLLBACK: Currency {amt} x {currency} to player {player}: {res2.Error.GenerateErrorReport()}");
						_analytics.EmitEvent("Item Vanished", new AnalyticsData()
						{
							{ "currency", currency.ToString() }, { "amount", amt.ToString() },
							{ "affectedPlayer", player }
						});
					}
				}

				throw;
			}
			_analytics.EmitUserEvent(player, "playfab_reward_event", new AnalyticsData()
			{
				{ "items_given", ModelSerializer.Serialize(givenGameItems).Value },
				{ "playfab_items", ModelSerializer.Serialize(consumedItems).Value },
			});
			return givenGameItems;
		}
	}
}