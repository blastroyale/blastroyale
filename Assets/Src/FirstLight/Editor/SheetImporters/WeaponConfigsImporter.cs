using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.GoogleSheetImporter;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class WeaponConfigsImporter : GoogleSheetConfigsAssetImporterBase<QuantumWeaponConfig, WeaponConfigs, CustomAssetConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=765551777";

		protected override QuantumWeaponConfig DeserializeAsset(Dictionary<string, string> data, CustomAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumWeaponConfig>(data);

			config.Specials = new List<GameId>
			{
				CsvParser.Parse<GameId>(data["SpecialId0"]), 
				CsvParser.Parse<GameId>(data["SpecialId1"])
			};

			return config;
		}
	}
}