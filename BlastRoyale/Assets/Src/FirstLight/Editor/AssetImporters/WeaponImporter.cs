using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace FirstLight.Editor.AssetImporters
{
	public class WeaponImporter : AssetPostprocessor
	{
		private const string WEAPON_PREFIX = "Weapon_";
		private const string ASSET_MANAGER_PATH = "Assets/Asset Manager";
		private const string WEAPONS_DIR = "Assets/AddressableResources/Weapons";

		public override int GetPostprocessOrder() => 100;

		private void OnPreprocessModel()
		{
			if (!Path.GetFileName(assetPath).StartsWith(WEAPON_PREFIX)) return;

			var importer = (ModelImporter) assetImporter;

			// Apply preset
			var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/WeaponFBX.preset");
			preset.ApplyTo(importer);

			// Find and map the M_Guns material
			importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
		}

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
												   string[] movedFromAssetPaths)
		{
			var weaponsAdded = false;

			foreach (var assetPath in importedAssets)
			{
				if (AssetDatabase.IsValidFolder(assetPath) && assetPath.StartsWith(ASSET_MANAGER_PATH))
				{
					// New asset has been imported
					var directoryName = Path.GetFileName(assetPath)!;

					if (directoryName.StartsWith(WEAPON_PREFIX))
					{
						var weaponName = directoryName[WEAPON_PREFIX.Length..];
						var weaponFBXFilename = $"{WEAPON_PREFIX}{weaponName}.fbx";
						var characterFBXPath = Path.Combine(assetPath, weaponFBXFilename);
						var characterPrefabFilename = $"{WEAPON_PREFIX}{weaponName}.prefab";
						var characterFBX = (GameObject) AssetDatabase.LoadMainAssetAtPath(characterFBXPath);

						Debug.Log($"Importing new weapon: {weaponName}");

						// We need to create a variant because for some reason when we set the animator controller
						// directly on the FBX it loses the reference.
						var weaponPrefab = (GameObject) PrefabUtility.InstantiatePrefab(characterFBX);

						// Create prefab variant
						PrefabUtility.SaveAsPrefabAssetAndConnect(weaponPrefab, Path.Combine(assetPath, characterPrefabFilename),
							InteractionMode.AutomatedAction, out var success);
						Object.DestroyImmediate(weaponPrefab);

						if (!success)
						{
							Debug.LogError($"Error creating prefab for weapon {weaponName}.");
							continue;
						}

						weaponsAdded = true;
					}
				}
			}

			// TODO: If we move the weapon directly here it loses the Asset Manager link, this is a workaround
			if (weaponsAdded)
			{
				DelayedEditorCall.DelayedCall(MoveWeapons, 0.5f);
			}
		}

		private static void MoveWeapons()
		{
			var folders = AssetDatabase.GetSubFolders(ASSET_MANAGER_PATH);

			foreach (var folder in folders)
			{
				var directoryName = Path.GetFileName(folder);
				var weaponName = directoryName[WEAPON_PREFIX.Length..];

				if (!directoryName.StartsWith(WEAPON_PREFIX)) continue;

				Debug.Log($"Moving weapon: {directoryName}");

				var destination = Path.Combine(WEAPONS_DIR, $"{weaponName}");
				var result = AssetDatabase.MoveAsset(folder, destination);
				if (!string.IsNullOrEmpty(result))
				{
					Debug.LogError($"Error moving weapon {weaponName}: {result}");
				}

				AssetDatabase.ImportAsset(destination, ImportAssetOptions.ImportRecursive);
			}
		}
	}
}