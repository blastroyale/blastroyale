using System;
using System.Collections.Generic;
using FirstLightEditor.GoogleSheetImporter;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	/// <remarks>
	/// This google sheet importer extends the behaviour to simplify the asset ref loading
	/// </remarks>
	public abstract class GoogleSheetConfigsAssetImporterBase<TConfig, TScriptableObject, TAssetConfigs> :
		GoogleSheetConfigsImporter<TConfig, TScriptableObject>
		where TConfig : struct
		where TScriptableObject : ScriptableObject, IConfigsContainer<TConfig>
		where TAssetConfigs : ScriptableObject
	{
		
		protected override void OnImport(TScriptableObject scriptableObject, List<Dictionary<string, string>> data)
		{
			var type = typeof(TAssetConfigs);
			var assets = AssetDatabase.FindAssets($"t:{type.Name}");
			var assetConfigs = AssetDatabase.LoadAssetAtPath<TAssetConfigs>(AssetDatabase.GUIDToAssetPath(assets[0]));
			var configs = new List<TConfig>();
			
			foreach (var row in data)
			{
				configs.Add(DeserializeAsset(row, assetConfigs));
			}

			scriptableObject.Configs = configs;
		}

		protected sealed override TConfig Deserialize(Dictionary<string, string> data)
		{
			throw new InvalidOperationException($"Use {nameof(DeserializeAsset)} instead");
		}

		protected abstract TConfig DeserializeAsset(Dictionary<string, string> data, TAssetConfigs assetConfigs);
	}
}