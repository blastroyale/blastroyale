using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLightServerSDK.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PlayFab;
using PlayFab.ServerModels;

namespace GameLogicService.Services;


public class PlayfabItemCatalogService : IItemCatalog<ItemData>
{
	private Dictionary<string, FlgCatalogItem> _catalog;
	private Cooldown _updateCooldown = new (TimeSpan.FromMinutes(1));
	private ILogger _log;

	public PlayfabItemCatalogService(ILogger log)
	{
		_log = log;
	}
	
	public async Task<FlgCatalogItem> GetCatalogItemById(string id)
	{
		await FetchCatalog();
		if (!_catalog.TryGetValue(id, out var catalogItem))
		{
			throw new Exception("Could not find catalog item for itemId " + id);
		}
		return catalogItem;
	}

	public async Task<ItemData> GetCatalogItem(string itemId)
	{
		var catalogItem = await GetCatalogItemById(itemId);
		var legacyData = ModelSerializer.Deserialize<LegacyItemData>(catalogItem.ItemData);
		return ItemFactory.Legacy(legacyData);
	}
	
	private async Task FetchCatalog()
	{
		if (_catalog == null || !_updateCooldown.IsCooldown())
		{
			_log.LogInformation("Refreshing cached items catalog");
			_updateCooldown.Trigger();
			var resultCatalog = await PlayFabServerAPI.GetCatalogItemsAsync(new()
			{
				CatalogVersion = "Store"
			});
			if (resultCatalog.Error != null) throw new Exception(resultCatalog.Error.GenerateErrorReport());
			_catalog = new();
			foreach (var catalogItem in resultCatalog.Result.Catalog)
			{
				_catalog[catalogItem.ItemId] = new FlgCatalogItem()
				{
					ItemData = catalogItem.CustomData,
					ItemId = catalogItem.ItemId
				};
			}
		}
	}
}