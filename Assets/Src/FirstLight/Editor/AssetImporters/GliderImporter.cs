using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Presets;
using UnityEngine;

namespace FirstLight.Editor.AssetImporters
{
	public class GliderImporter : AssetPostprocessor
	{
		private const string GLIDER_PREFIX = "Glider_";
		private const string ASSET_MANAGER_PATH = "Assets/Asset Manager";
		private const string GLIDERS_DIR = "Assets/AddressableResources/Gliders";

		public override int GetPostprocessOrder() => 100;

		private void OnPreprocessModel()
		{
			if (!Path.GetFileName(assetPath).StartsWith(GLIDER_PREFIX)) return;

			var importer = (ModelImporter) assetImporter;

			// Extract textures
			var folder = assetPath!.Remove(assetPath.LastIndexOf('/'));
			importer.ExtractTextures(folder);

			// Apply preset
			var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/GliderFBX.preset");
			preset.ApplyTo(importer);
		}

		private void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
		{
			if (!Path.GetFileName(assetPath).StartsWith(GLIDER_PREFIX)) return;

			// We set the flames material manually in the preset
			if (description.materialName == "M_Glider_Flames") return;

			material.shader = Shader.Find("FLG/Unlit/Dynamic Object");

			if (description.TryGetProperty("DiffuseColor", out TexturePropertyDescription diffuseColor))
			{
				material.SetTexture(Shader.PropertyToID("_MainTex"), diffuseColor.texture);
			}
		}

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
												   string[] movedFromAssetPaths)
		{
			var glidersAdded = false;

			foreach (var assetPath in importedAssets)
			{
				if (AssetDatabase.IsValidFolder(assetPath) && assetPath.StartsWith(ASSET_MANAGER_PATH))
				{
					// New asset has been imported
					var directoryName = Path.GetFileName(assetPath)!;

					if (directoryName.StartsWith(GLIDER_PREFIX))
					{
						var gliderName = directoryName[GLIDER_PREFIX.Length..];
						var gliderFBXFilename = $"{GLIDER_PREFIX}{gliderName}.fbx";
						var gliderFBXPath = Path.Combine(assetPath, gliderFBXFilename);
						var gliderPrefabFilename = $"{GLIDER_PREFIX}{gliderName}.prefab";
						var gliderFBX = (GameObject) AssetDatabase.LoadMainAssetAtPath(gliderFBXPath);

						Debug.Log($"Importing new glider: {gliderName}");

						// We need to create a variant because for some reason when we set the animator controller
						// directly on the FBX it loses the reference.
						var gliderPrefab = (GameObject) PrefabUtility.InstantiatePrefab(gliderFBX);

						// Create prefab variant
						PrefabUtility.SaveAsPrefabAssetAndConnect(gliderPrefab, Path.Combine(assetPath, gliderPrefabFilename),
							InteractionMode.AutomatedAction, out var success);
						Object.DestroyImmediate(gliderPrefab);

						if (!success)
						{
							Debug.LogError($"Error creating prefab for glider {gliderName}.");
							continue;
						}

						glidersAdded = true;
					}
				}
			}

			// TODO: If we move the glider directly here it loses the Asset Manager link, this is a workaround
			if (glidersAdded)
			{
				DelayedEditorCall.DelayedCall(MoveGliders, 0.5f);
			}
		}

		private static void MoveGliders()
		{
			var folders = AssetDatabase.GetSubFolders(ASSET_MANAGER_PATH);

			foreach (var folder in folders)
			{
				var directoryName = Path.GetFileName(folder);
				var gliderName = directoryName[GLIDER_PREFIX.Length..];

				if (!directoryName.StartsWith(GLIDER_PREFIX)) continue;

				Debug.Log($"Moving glider: {directoryName}");

				var destination = Path.Combine(GLIDERS_DIR, $"{gliderName}");
				var result = AssetDatabase.MoveAsset(folder, destination);
				if (!string.IsNullOrEmpty(result))
				{
					Debug.LogError($"Error moving glider {gliderName}: {result}");
				}

				AssetDatabase.ImportAsset(destination, ImportAssetOptions.ImportRecursive);
			}
		}
	}
}