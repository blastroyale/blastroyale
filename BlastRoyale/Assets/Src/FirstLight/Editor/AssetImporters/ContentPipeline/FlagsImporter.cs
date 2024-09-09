using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Presets;
using UnityEngine;

namespace FirstLight.Editor.AssetImporters
{
	public class FlagsImporter : AssetPostprocessor
	{
		private const string PREFIX = "flag_";
		private const string ASSET_MANAGER_PATH = "Assets/Asset Manager";
		private const string ADDRESSABLE_DIR = "Assets/AddressableResources/Collections/Flags";
		private const string MATERIAL_DIR = "Assets/AddressableResources/Collections/Flags/m_FlagsAtlas.mat";
		
		public override int GetPostprocessOrder() => 100;

		private void OnPreprocessModel()
		{
			if (!Path.GetFileName(assetPath).StartsWith(PREFIX)) return;

			var importer = (ModelImporter) assetImporter;

			// Extract textures
			var folder = assetPath!.Remove(assetPath.LastIndexOf('/'));
			importer.ExtractTextures(folder);

			// Apply preset
			var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/FlagsFBX.preset");
			preset.ApplyTo(importer);
		}
		

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
												   string[] movedFromAssetPaths)
		{
			var assetsAdded = false;

			var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_DIR);
			
			foreach (var assetPath in importedAssets)
			{
				if (AssetDatabase.IsValidFolder(assetPath) && assetPath.StartsWith(ASSET_MANAGER_PATH))
				{
					// New asset has been imported
					var directoryName = Path.GetFileName(assetPath)!;

					if (directoryName.StartsWith(PREFIX))
					{
						var assetName = directoryName[PREFIX.Length..];
						var assetFbxFilename = $"{PREFIX}{assetName}.fbx";
						var assetFbxPath = Path.Combine(assetPath, assetFbxFilename);
						var assetPrefabFilename = $"{PREFIX}{assetName}.prefab";
						var assetFbx = (GameObject) AssetDatabase.LoadMainAssetAtPath(assetFbxPath);

						Debug.Log($"Importing new asset: {assetName}");

						
						// We need to create a variant because for some reason when we set the animator controller
						// directly on the FBX it loses the reference.
						var instantiatedPrefab = (GameObject) PrefabUtility.InstantiatePrefab(assetFbx);
						instantiatedPrefab.GetComponent<MeshRenderer>().sharedMaterial = material;
						
						// Create prefab variant
						PrefabUtility.SaveAsPrefabAssetAndConnect(instantiatedPrefab, Path.Combine(assetPath, assetPrefabFilename),
							InteractionMode.AutomatedAction, out var success);
						Object.DestroyImmediate(instantiatedPrefab);

						if (!success)
						{
							Debug.LogError($"Error creating prefab for {assetName}.");
							continue;
						}

						assetsAdded = true;
					}
				}
			}

			// TODO: If we move the asset directly here it loses the Asset Manager link, this is a workaround
			if (assetsAdded)
			{
				DelayedEditorCall.DelayedCall(MoveAssets, 0.5f);
			}
		}

		private static void MoveAssets()
		{
			var folders = AssetDatabase.GetSubFolders(ASSET_MANAGER_PATH);

			foreach (var folder in folders)
			{
				var directoryName = Path.GetFileName(folder);
				var gliderName = directoryName[PREFIX.Length..];

				if (!directoryName.StartsWith(PREFIX)) continue;

				Debug.Log($"Moving asset: {directoryName}");

				var destination = Path.Combine(ADDRESSABLE_DIR, $"{gliderName}");
				var result = AssetDatabase.MoveAsset(folder, destination);
				if (!string.IsNullOrEmpty(result))
				{
					Debug.LogError($"Error moving asset {gliderName}: {result}");
				}

				AssetDatabase.ImportAsset(destination, ImportAssetOptions.ImportRecursive);
			}
		}
	}
}