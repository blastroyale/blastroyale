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
	public class DestructibleConfigsImporter : GoogleSheetConfigsAssetImporterBase<QuantumDestructibleConfig, DestructibleConfigs, QuantumPrototypeAssetConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=538072828";
		
		protected override QuantumDestructibleConfig DeserializeAsset(Dictionary<string, string> data, QuantumPrototypeAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumDestructibleConfig>(data);

			if (assetConfigs.ConfigsDictionary.TryGetValue(config.Id, out var assetReference))
			{
				config.AssetRef = QuantumConverter.QuantumEntityRef(AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID));
			}
			
			if (assetConfigs.ConfigsDictionary.TryGetValue(CsvParser.Parse<GameId>(data["ProjectileId"]), out var projectileAssetReference))
			{
				config.ProjectileAssetRef = QuantumConverter.QuantumEntityRef(AssetDatabase.GUIDToAssetPath(projectileAssetReference.AssetGUID));
			}

			return config;
		}
	}
}