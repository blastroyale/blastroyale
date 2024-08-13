using System;
using System.Collections.Generic;
using System.IO;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using Quantum;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using LayerMask = UnityEngine.LayerMask;
using Object = UnityEngine.Object;

namespace FirstLight.Editor.AssetImporters
{
	public class ContentPipeline
	{
		public ContentPipelineConfig Config { get; set; }

		public Action OnFinishedImport;
		public Action<GameObject> OnFinishedModel;
		
		/// <summary>
		/// Move assets from the asset manager folder to the collection folder
		/// </summary>
		public void MoveAssets()
		{
			var folders = AssetDatabase.GetSubFolders(Config.ImportPath);
			foreach (var folder in folders)
			{
				var directoryName = Path.GetFileName(folder);
				if (directoryName.Length < Config.Prefix.Length) continue;
				
				var assetName = directoryName[Config.Prefix.Length..];

				if (!directoryName.StartsWith(Config.Prefix)) continue;


				var destination = Path.Combine(Config.AssetDir, $"{assetName}");
				Debug.Log($"Moving asset: {directoryName} to {destination}");

				AssetDatabase.DeleteAsset(destination);
			
				var result = AssetDatabase.MoveAsset(folder, destination);
				if (!string.IsNullOrEmpty(result))
				{
					Debug.LogError($"Error moving asset {assetName}: {result}");
				}
				AssetDatabase.ImportAsset(destination, ImportAssetOptions.ImportRecursive);
			}
			
			OnFinishedImport?.Invoke();
		}

		public void OnProcessModel(UnityEditor.AssetImporter assetImporter, string assetPath)
		{
			if (!CanProccessModel(assetPath)) return;

			if (string.IsNullOrEmpty(Config.Preset)) return;
			
			var importer = (ModelImporter) assetImporter;

			// Apply preset
			if (assetPath.StartsWith("Icon_"+Config.Prefix))
			{
				var iconPreset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/Icons.preset");
				iconPreset.ApplyTo(importer);
			}
			else
			{
				var preset = AssetDatabase.LoadAssetAtPath<Preset>(Config.Preset);
				preset.ApplyTo(importer);
			}

			// Find and map the M_Guns material
			importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
		}
		
		public List<GameObject> OnImportAssets(string[] importedAssets, Action<GameObject> onCreate)
		{
			var made = new List<GameObject>();
			var wasAdded = false;
			foreach (var assetPath in importedAssets)
			{
			
				if (AssetDatabase.IsValidFolder(assetPath) && !assetPath.StartsWith(Config.AssetDir))
				{
					// New asset has been imported
					var directoryName = Path.GetFileName(assetPath)!;

					if (directoryName.StartsWith(Config.Prefix))
					{
						var assetName = directoryName[Config.Prefix.Length..];
						var fbxFilename = $"{Config.Prefix}{assetName}.fbx";
						var fbxPath = Path.Combine(assetPath, fbxFilename);
						var prefabFilename = $"{Config.Prefix}{assetName}.prefab";
						var assetFbx = (GameObject) AssetDatabase.LoadMainAssetAtPath(fbxPath);

						Debug.Log($"Importing new asset: {assetName}");
						try
						{

							// We need to create a variant because for some reason when we set the animator controller
							// directly on the FBX it loses the reference.
							var generatedPrefab = (GameObject) PrefabUtility.InstantiatePrefab(assetFbx);

							onCreate?.Invoke(generatedPrefab);

							// Create prefab variant
							var o = PrefabUtility.SaveAsPrefabAssetAndConnect(generatedPrefab, Path.Combine(assetPath, prefabFilename),
								InteractionMode.AutomatedAction, out var success);
							Object.DestroyImmediate(generatedPrefab);
							
							

							made.Add(o);
							if (!success)
							{
								Debug.LogError($"Error creating prefab for asset {assetName}.");
								continue;
							}

							wasAdded = true;
						}
						catch (Exception e)
						{
							EditorUtility.DisplayDialog($"Failed to import {assetName} please check format with art", e.Message, "Aight");
							Log.Exception(e);
						}
					}
				}
			}

			// TODO: If we move the weapon directly here it loses the Asset Manager link, this is a workaround
			if (wasAdded)
			{
				DelayedEditorCall.DelayedCall(MoveAssets, 0.5f);
			}
			return made;
		}

		public bool CanProccessModel(string assetPath)
		{
			if (assetPath.Contains("Legacy")) return false; // Temporary while we have old
			if (!Path.GetFileName(assetPath).StartsWith(Config.Prefix)) return false;
			return true;
		}
		
		public void OnPreprocessTexture(string assetPath, UnityEditor.AssetImporter assetImporter)
		{
			if (!assetPath.StartsWith(Config.AssetDir)) return;

			var importer = (TextureImporter) assetImporter;
			var filename = Path.GetFileName(assetPath);
			if (filename.StartsWith("Icon_"+Config.Prefix))
			{
				var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/Icons.preset");
				preset.ApplyTo(importer);
			}
		}
		
		public void OnPostprocessModel(string asssetPath, GameObject g)
		{
			if (!Path.GetFileName(asssetPath).StartsWith(Config.Prefix)) return;
			if (Config.Layer != null)
			{
				g.SetLayer(LayerMask.NameToLayer(Config.Layer));
			}
		}
		
		private static GameId? NameToGameId(string prefix, string name)
		{
			if (Enum.TryParse(typeof(GameId), $"{prefix}{name}", true, out var gid))
			{
				return (GameId) gid;
			}

			return name switch
			{
				"AssassinMale"       => GameId.MaleAssassin,
				"AssassinFemale"     => GameId.FemaleAssassin,
				"CorposFemale"       => GameId.FemaleCorpos,
				"CorposMale"         => GameId.MaleCorpos,
				"PunkFemale"         => GameId.FemalePunk,
				"PunkMale"           => GameId.MalePunk,
				"SuperstarFemale"    => GameId.FemaleSuperstar,
				"SuperstarMale"      => GameId.MaleSuperstar,
				"SuperstarMale_Xmas" => GameId.PlayerSkinXmasSuperstar,
				_                    => null
			};
		}
	}
}