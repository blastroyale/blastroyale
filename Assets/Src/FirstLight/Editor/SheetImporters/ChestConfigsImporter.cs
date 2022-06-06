using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	public class ChestConfigsImporter :
		GoogleSheetConfigsAssetImporterBase<QuantumChestConfig, ChestConfigs, CustomAssetConfigs>
	{
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=1440451349";

		protected override QuantumChestConfig DeserializeAsset(Dictionary<string, string> data,
		                                                       CustomAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumChestConfig>(data);

			config.AssetRef = assetConfigs.ChestPrototype;

			return config;
		}
	}
}