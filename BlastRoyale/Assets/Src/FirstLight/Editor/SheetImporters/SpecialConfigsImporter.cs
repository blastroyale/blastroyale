using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class SpecialConfigsImporter : GoogleSheetConfigsAssetImporterBase<QuantumSpecialConfig, SpecialConfigs, QuantumPrototypeAssetConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1389279474";
		
		protected override QuantumSpecialConfig DeserializeAsset(Dictionary<string, string> data, QuantumPrototypeAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumSpecialConfig>(data);
			
			return config;
		}
	}
}