using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Utils;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEditor;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class WeaponConfigsImporter : GoogleSheetConfigsAssetImporterBase<QuantumWeaponConfig, WeaponConfigs, CustomAssetConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1031494639";

		protected override QuantumWeaponConfig DeserializeAsset(Dictionary<string, string> data, CustomAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumWeaponConfig>(data);

			config.Specials = new List<GameId>
			{
				CsvParser.Parse<GameId>(data["SpecialId0"]), 
				CsvParser.Parse<GameId>(data["SpecialId1"])
			};

			config.AssetRef = assetConfigs.WeaponPickUpPrototype;

			return config;
		}
	}
}