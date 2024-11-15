using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Editor.AssetImporters
{
	public class FlagsImporter : AssetPostprocessor
	{
		private static List<AssetDetails> _importedAssets;
		private const string PREFIX = "Flag_";
		private const string ADDRESSABLE_DIR = "Assets/AddressableResources/Collections/Flags";
		private const string PREFAB_FILE = "Assets/AddressableResources/Collections/Flags/BaseDeathFlag.prefab";
		
		public override int GetPostprocessOrder() => 100;

		private void OnPreprocessModel()
		{
			Debug.Log($"is it a flag? {Path.GetFileName(assetPath)} | {Path.GetFileName(assetPath).StartsWith(PREFIX)}");
			if (!Path.GetFileName(assetPath).StartsWith(PREFIX)) return;
			
			var importer = (ModelImporter) assetImporter;
			
			// Extract textures
			var folder = assetPath!.Remove(assetPath.LastIndexOf('/'));
			importer.ExtractTextures(folder);
			
			// Apply preset
			var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/FlagFBX.preset");
			preset.ApplyTo(importer);
		}


		private class AssetDetails
		{
			public string Destination;
			public string AssetPath;
			public string ImportDirectory;
		}
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
												   string[] movedFromAssetPaths)
		{
			
			var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_FILE);
			_importedAssets = new List<AssetDetails>();
			foreach (var assetPath in importedAssets)
			{
				if (AssetDatabase.IsValidFolder(assetPath))
				{
					Debug.Log(assetPath);
					var directory = new DirectoryInfo(assetPath);
					var parent = directory.Parent;
					if (parent == null) continue;
					var isImportedFromAssetManager = parent.Name == "Assets";
					Debug.Log($"Check asset : {EditorPrefs.GetString("importSettingsDefaultLocationLabel")} | {assetPath} | {parent.Name} | {isImportedFromAssetManager} | {AssetDatabase.IsValidFolder(assetPath)}");
					if(!isImportedFromAssetManager) continue;
					
					// New asset has been imported
					var directoryName = Path.GetFileName(assetPath)!;

					if (directoryName.StartsWith(PREFIX))
					{
						var assetName = Path.GetFileNameWithoutExtension(assetPath);
						var assetFbxFilename = $"{assetName}.fbx";
						var assetFbxPath = $"{assetPath}/{assetFbxFilename}";
						var assetPrefabFilename = $"{assetName}.prefab";
						var destination = $"{ADDRESSABLE_DIR}/{assetName}";
						if (!Directory.Exists(destination))
						{
							Directory.CreateDirectory(destination);
						}
						Debug.Log($"Importing new asset: {destination} | {assetName} | {assetFbxPath} | {Path.Combine(destination, assetFbxFilename)}");

						// We need to create a variant because for some reason when we set the animator controller
						// directly on the FBX it loses the reference.
						var instantiatedPrefab = (GameObject) PrefabUtility.InstantiatePrefab(basePrefab);
						var meshRenderer = instantiatedPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
						var assetFbx = AssetDatabase.LoadAllAssetsAtPath(assetFbxPath);
						foreach (var asset in assetFbx)
						{
							if (asset is Mesh)
							{
								Debug.Log($"Applying mesh {assetFbxPath} | {assetFbx}");
						
								meshRenderer.sharedMesh = asset as Mesh;
							}
						}

						// Create prefab variant
						PrefabUtility.SaveAsPrefabAssetAndConnect(instantiatedPrefab, $"{destination}/{assetPrefabFilename}",
							InteractionMode.AutomatedAction, out var success);
						Object.DestroyImmediate(instantiatedPrefab);
						_importedAssets.Add(new AssetDetails{AssetPath = assetFbxPath, Destination = $"{destination}/{assetFbxFilename}", ImportDirectory = assetPath});
						
						if (!success)
						{
							Debug.LogError($"Error creating prefab for {assetName}.");
							continue;
						}
					}
				}
			}
			
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			MoveAssets();
		}

		private static void MoveAssets()
		{
			foreach (var asset in _importedAssets)
			{
				AssetDatabase.MoveAsset(asset.AssetPath, asset.Destination);
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			foreach (var asset in _importedAssets)
			{
				Debug.Log(asset.AssetPath);
				AssetDatabase.DeleteAsset(asset.ImportDirectory);
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}