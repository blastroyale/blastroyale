using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.GoogleSheetImporter;
using FirstLightEditor.GoogleSheetImporter;
using I2.Loc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using Quantum;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	[GoogleSheetImportOrder(1)]
	public class StoreConfigsImporter : IGoogleSheetConfigsImporter
	{
		/// <inheritdoc />
		public string GoogleSheetUrl => "https://docs.google.com/spreadsheets/d/1TZuc8gOMgrN6nJWRFJymxmf2SR2QNyQfx0x-STtIN-M/edit#gid=1971354923";

		/// <inheritdoc />
		public void Import(List<Dictionary<string, string>> data)
		{
#if ENABLE_PLAYFABADMIN_API
			var converter = new StringEnumConverter();
			var request = new PlayFab.AdminModels.UpdateCatalogItemsRequest
			{
				CatalogVersion = StoreService.StoreCatalogVersion,
				Catalog = new List<PlayFab.AdminModels.CatalogItem>()
			};

			foreach (var row in data)
			{
				var itemId = row[nameof(PlayFab.AdminModels.CatalogItem.ItemId)];
				var customData = new CatalogItemCustomData
				{
					ItemGameId = CsvParser.Parse<GameId>(row[nameof(CatalogItemCustomData.ItemGameId)]),
					RewardGameId = CsvParser.Parse<GameId>(row[nameof(CatalogItemCustomData.RewardGameId)]),
					PriceGameId = CsvParser.Parse<GameId>(row[nameof(CatalogItemCustomData.PriceGameId)]),
					PriceValue = CsvParser.Parse<float>(row[nameof(CatalogItemCustomData.PriceValue)]),
					RewardValue = CsvParser.Parse<uint>(row[nameof(CatalogItemCustomData.RewardValue)]),
				};
				var item = new PlayFab.AdminModels.CatalogItem
				{
					DisplayName = LocalizationManager.GetTranslation($"Shop/{itemId}"),
					ItemId = row[nameof(PlayFab.AdminModels.CatalogItem.ItemId)],
					ItemClass = StoreService.StoreCatalogVersion,
					CustomData = JsonConvert.SerializeObject(customData, converter),
					Consumable = new PlayFab.AdminModels.CatalogItemConsumableInfo { UsageCount = 1 }
				};

				request.Catalog.Add(item);
			}

			PlayFabAdminAPI.SetCatalogItems(request, 
			                                result => Debug.Log($"{request.Catalog.Count} items added to the store catalog"), 
			                                error => Debug.LogError(error.ErrorMessage));
#endif
		}
	}
}