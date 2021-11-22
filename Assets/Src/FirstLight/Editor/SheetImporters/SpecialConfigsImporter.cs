using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEditor;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class SpecialConfigsImporter : GoogleSheetConfigsAssetImporterBase<QuantumSpecialConfig, SpecialConfigs, QuantumPrototypeAssetConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "https://docs.google.com/spreadsheets/d/1TZuc8gOMgrN6nJWRFJymxmf2SR2QNyQfx0x-STtIN-M/edit#gid=717164846";
		
		protected override QuantumSpecialConfig DeserializeAsset(Dictionary<string, string> data, QuantumPrototypeAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumSpecialConfig>(data);
			
			return config;
		}
	}
}