using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	public class MutatorConfigsImporter :
		GoogleSheetConfigsAssetImporterBase<QuantumMutatorConfig, MutatorConfigs, CustomAssetConfigs>
	{
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=54961578";

		protected override QuantumMutatorConfig DeserializeAsset(Dictionary<string, string> data,
		                                                         CustomAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumMutatorConfig>(data);

			return config;
		}
	}
}