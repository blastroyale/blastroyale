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
	public class ConsumableConfigsImporter : GoogleSheetConfigsAssetImporterBase<QuantumConsumableConfig, ConsumableConfigs, CustomAssetConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1668589781";

		protected override QuantumConsumableConfig DeserializeAsset(Dictionary<string, string> data, CustomAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumConsumableConfig>(data);

			config.AssetRef = assetConfigs.ConsumablePrototype;
			
			return config;
		}
	}
}