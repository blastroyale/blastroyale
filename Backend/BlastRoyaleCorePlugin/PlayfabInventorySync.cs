using System;
using System.Collections.Generic;
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

public static class PlayfabCurrencies
{
	public static string COINS = "CN";
	public static string CRAFT_SPICE = "CS";
}

/// <summary>
/// Syncs playfab inventory items with user data
/// It will remove any pending items and currency from playfab inventory
/// and convert to our currency datas
/// </summary>
public class PlayfabInventorySync : BaseEventDataSync<InventoryUpdatedEvent>
{
	private IServerStateService _serverState;
	private IServerMutex _mutex;
	private IPluginLogger _log;
	private Dictionary<string, CatalogItem> _catalog;
	
	public PlayfabInventorySync(PluginContext ctx) : base(ctx)
	{
		_serverState = ctx.ServerState;
		_mutex = ctx.PlayerMutex;
		_log = ctx.Log;
	}

	public async Task<CatalogItem> GetCatalogItem(ItemInstance item)
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

	public override async Task<bool> SyncData(string player)
	{
		var consumedItems = new List<ItemInstance>();
		var consumedCurrencies = new Dictionary<string, int>();
		
		try
		{
			await _mutex.Lock(player);
			var result = await PlayFabServerAPI.GetUserInventoryAsync(new()
			{
				PlayFabId = player, 
			});
			if (result.Error != null) throw new Exception(result.Error.GenerateErrorReport());
			var inventory = result.Result;
			
			inventory.VirtualCurrency.TryGetValue(PlayfabCurrencies.COINS, out var coins);
			inventory.VirtualCurrency.TryGetValue(PlayfabCurrencies.CRAFT_SPICE, out var cs);
			var state = await _serverState.GetPlayerState(player);
			var playerData = state.DeserializeModel<PlayerData>();

			if (coins > 0)
			{
				playerData.Currencies[GameId.COIN] += (uint) coins;
				var res = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(new()
				{
					Amount = coins,
					PlayFabId = player,
					VirtualCurrency = PlayfabCurrencies.COINS
				});
				if (res.Error != null) throw new Exception(res.Error.GenerateErrorReport());
				consumedCurrencies[PlayfabCurrencies.COINS] = cs;
				_log.LogInformation($"Synced {coins} coins for user {player}");
			}
			if (cs > 0)
			{
				playerData.Currencies[GameId.CS] += (uint) cs;
				var res = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(new()
				{
					Amount = cs,
					PlayFabId = player,
					VirtualCurrency = PlayfabCurrencies.CRAFT_SPICE
				});
				if (res.Error != null) throw new Exception(res.Error.GenerateErrorReport());
				consumedCurrencies[PlayfabCurrencies.CRAFT_SPICE] = cs;
				_log.LogInformation($"Synced {cs} CS for user {player}");
			}

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
					_log.LogInformation($"Synced item {item.DisplayName} to player {player}");
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
			_log.LogError("Exception when syncing inventory, rolling back items removed");
			var itemIds = consumedItems.Select(e => e.ItemId).ToList();
			if (itemIds.Count > 0)
			{
				var res = await PlayFabServerAPI.GrantItemsToUserAsync(new()
				{
					ItemIds = itemIds,
					PlayFabId = player
				});
				if(res.Error != null) _log.LogError($"CRITICAL ON ITEM ROLLBACK: Items {string.Join(",", itemIds)} to player {player}: {res.Error.GenerateErrorReport()}");
			}
			
			foreach (var (currency, amt) in consumedCurrencies)
			{
				var res2 = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new ()
				{
					Amount = amt, PlayFabId = player, VirtualCurrency = currency
				});
				if(res2.Error != null) _log.LogError($"CRITICAL ON CURRENCY ROLLBACK: Currency {amt} x {currency} to player {player}: {res2.Error.GenerateErrorReport()}");
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

