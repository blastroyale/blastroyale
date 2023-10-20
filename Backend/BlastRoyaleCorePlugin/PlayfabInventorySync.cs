using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using Quantum;
using ItemInstance = PlayFab.ServerModels.ItemInstance;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Map of game id currencies to playfab currency names
	/// </summary>
	public static class PlayfabCurrencies
	{
		public static readonly IReadOnlyDictionary<GameId, string> MAP = new Dictionary<GameId, string>()
		{
			{ GameId.COIN, "CN" },
			{ GameId.CS, "CS" },
			{ GameId.XP, "XP" }
		};
	}

	/// <summary>
	/// Syncs playfab inventory items with user data
	/// It will remove any pending items and currency from playfab inventory
	/// and convert to our currency datas
	/// </summary>
	public class PlayfabInventorySync : BaseEventDataSync<PlayerDataLoadEvent>
	{
		private PluginContext _ctx;
		private IServerStateService _serverState;
		private IServerMutex _mutex;
		private IPluginLogger _log;
		private Dictionary<string, CatalogItem> _catalog;
		
		public PlayfabInventorySync(PluginContext ctx) : base(ctx)
		{
			_ctx = ctx;
			_serverState = ctx.ServerState;
			_mutex = ctx.PlayerMutex;
			_log = ctx.Log;
		}

		private async Task<CatalogItem> GetCatalogItem(ItemInstance item)
		{
			if (_catalog == null)
			{
				var resultCatalog = await PlayFabServerAPI.GetCatalogItemsAsync(new()
				{
					CatalogVersion = "Store"
				});
				if (resultCatalog.Error != null) throw new Exception(resultCatalog.Error.GenerateErrorReport());
				_catalog = new();
				foreach (var catalogItem in resultCatalog.Result.Catalog)
				{
					_catalog[catalogItem.ItemId] = catalogItem;
				}
			}
			return _catalog[item.ItemId];
		}
		
		private async Task<int> SyncCurrency(string player, GetUserInventoryResult inventory, PlayerData playerData, GameId gameId)
		{
			var playfabName = PlayfabCurrencies.MAP[gameId];
			inventory.VirtualCurrency.TryGetValue(playfabName, out var playfabAmount);
			if (playfabAmount > 0)
			{
				playerData.Currencies.TryGetValue(gameId, out var currentAmt);
				currentAmt += (uint) playfabAmount;
				playerData.Currencies[gameId] = currentAmt;
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

		public override async Task<bool> SyncData(string player)
		{
			var consumedItems = new List<ItemInstance>();
			var consumedCurrencies = new Dictionary<GameId, int>();
			try
			{
				await _mutex.Lock(player);
				var result = await PlayFabServerAPI.GetUserInventoryAsync(new()
				{
					PlayFabId = player, 
				});
				if (result.Error != null) throw new Exception(result.Error.GenerateErrorReport());
				var inventory = result.Result;
				
				var state = await _serverState.GetPlayerState(player);
				var playerData = state.DeserializeModel<PlayerData>();

				consumedCurrencies[GameId.COIN] = await SyncCurrency(player, inventory, playerData, GameId.COIN);
				consumedCurrencies[GameId.CS] = await SyncCurrency(player, inventory, playerData, GameId.CS);

				if (inventory.Inventory.Count > 0)
				{
					foreach (var item in inventory.Inventory)
					{
						var catalogItem = await GetCatalogItem(item);
						var legacy = JsonConvert.DeserializeObject<LegacyItemData>(catalogItem.CustomData);
						playerData.UncollectedRewards.Add(ItemFactory.Legacy(legacy));
						var res = await PlayFabServerAPI.ConsumeItemAsync(new ConsumeItemRequest
							{ConsumeCount = 1, PlayFabId = player, ItemInstanceId = item.ItemInstanceId});
						if (res.Error != null) throw new Exception(res.Error.GenerateErrorReport());
						consumedItems.Add(item);
						_log.LogInformation($"[Playfab Sync] Synced item {item.DisplayName} -> {legacy.RewardId} to player {player}");
					}
				}
				
				state.UpdateModel(playerData);
				if (state.HasDelta())
				{
					await _serverState.UpdatePlayerState(player, state.GetOnlyUpdatedState());
				}
			}
			catch (Exception e)
			{
				_log.LogError(e.Message + e.StackTrace);
				_log.LogError("[Playfab Sync] Exception when syncing inventory, rolling back items removed");
				var itemIds = consumedItems.Select(e => e.ItemId).ToList();
				if (itemIds.Count > 0)
				{
					var res = await PlayFabServerAPI.GrantItemsToUserAsync(new()
					{
						ItemIds = itemIds,
						PlayFabId = player
					});
					if (res.Error != null)
					{
						var itemsString = string.Join(",", itemIds);
						_log.LogError($"CRITICAL ON ITEM ROLLBACK: Items {itemsString} to player {player}: {res.Error.GenerateErrorReport()}");
						_ctx.Analytics.EmitEvent("Item Vanished", new AnalyticsData()
						{
							{"items", itemsString}, {"affectedPlayer", player}
						});
					}
				}
				
				foreach (var (currency, amt) in consumedCurrencies)
				{
					if (amt == 0) continue;
					var res2 = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new ()
					{
						Amount = amt, PlayFabId = player, VirtualCurrency = PlayfabCurrencies.MAP[currency]
					});
					if (res2.Error != null)
					{
						_log.LogError($"CRITICAL ON CURRENCY ROLLBACK: Currency {amt} x {currency} to player {player}: {res2.Error.GenerateErrorReport()}");
						_ctx.Analytics.EmitEvent("Item Vanished", new AnalyticsData()
						{
							{"currency", currency}, {"amount", amt}, {"affectedPlayer", player}
						});
					}
				}
			}
			finally
			{
				_mutex.Unlock(player);
			}
			return true;
		}
	}
}

