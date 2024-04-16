using System.IO;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.Utils;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Presets;
using UnityEngine;

namespace FirstLight.Editor.AssetImporters
{
	public class CharacterImporter : AssetPostprocessor
	{
		private const string CHAR_PATH = "Assets/AddressableResources/Collections/CharacterSkins";

		private void OnPreprocessModel()
		{
			if (!assetPath.StartsWith(CHAR_PATH)) return;
			if (!Path.GetFileName(assetPath).StartsWith("Char_")) return;

			var importer = (ModelImporter) assetImporter;

			// Extract textures
			var folder = assetPath.Remove(assetPath.LastIndexOf('/'));
			importer.ExtractTextures(folder);

			// Apply preset
			var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/CharacterFBX.preset");
			preset.ApplyTo(importer);
		}

		private void OnPreprocessAnimation()
		{
			if (!assetPath.StartsWith(CHAR_PATH)) return;

			// Set defaults for character animation clips.

			var importer = (ModelImporter) assetImporter;

			var clips = importer.clipAnimations;
			foreach (var anim in clips)
			{
				anim.lockRootRotation = true;
				anim.lockRootHeightY = true;
				anim.lockRootPositionXZ = true;
				anim.keepOriginalOrientation = true;
				anim.keepOriginalPositionY = true;
				anim.keepOriginalPositionXZ = true;

				if (anim.name.EndsWith("_loop"))
				{
					anim.loopTime = true;
					anim.loopPose = true;
				}
			}

			importer.clipAnimations = clips;
		}

		private void OnPreprocessTexture()
		{
			if (!assetPath.StartsWith(CHAR_PATH)) return;

			var importer = (TextureImporter) assetImporter;
			var filename = Path.GetFileName(assetPath);

			if (filename.StartsWith("T_Char_"))
			{
				var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/CharacterTexture.preset");
				preset.ApplyTo(importer);
			}
			else if (filename.StartsWith("Icon_Char_"))
			{
				var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Presets/CharacterIcon.preset");
				preset.ApplyTo(importer);
			}
			else
			{
				context.LogImportWarning($"Unknown texture in characters folder: {assetPath}");
			}
		}

		private void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
		{
			if (!assetPath.StartsWith(CHAR_PATH)) return;
			if (!Path.GetFileName(assetPath).StartsWith("Char_")) return;

			material.shader = Shader.Find("FLG/Unlit/Dynamic Object");

			if (description.TryGetProperty("DiffuseColor", out TexturePropertyDescription diffuseColor))
			{
				material.SetTexture(Shader.PropertyToID("_MainTex"), diffuseColor.texture);
			}
		}

		/// <summary>
		/// Sets embedded material to correct shader
		/// </summary>
		private void OnPostprocessModel(GameObject g)
		{
			if (!assetPath.StartsWith(CHAR_PATH)) return;
			if (!Path.GetFileName(assetPath).StartsWith("Char_")) return;

			g.AddComponent<CharacterSkinMonoComponent>().SetupReferences();

			g.GetComponent<Animator>().applyRootMotion = false;
			g.GetComponent<Animator>().runtimeAnimatorController =
				AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{CHAR_PATH}/Shared/character_animator.controller");

			g.SetLayer(LayerMask.NameToLayer("Players"));
		}

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
												   string[] movedFromAssetPaths)
		{
			foreach (var asset in importedAssets)
			{
				if (asset.StartsWith(CHAR_PATH)) continue;

				// Check if any of the imported FBX's are new characters
				var filename = Path.GetFileName(asset);
				if (filename.StartsWith("Char_") && filename.EndsWith(".fbx"))
				{
					var characterName = filename.Substring(5, filename.Length - 9);
					var destFolder = Path.Combine(CHAR_PATH, characterName);

					if (AssetDatabase.IsValidFolder(destFolder))
					{
						var destFile = Path.Combine(destFolder, filename);
						File.Copy(asset, destFile, true);
						AssetDatabase.ImportAsset(destFile);
						AssetDatabase.DeleteAsset(asset);
					}
					else
					{
						var folderGuid = AssetDatabase.CreateFolder(CHAR_PATH, characterName);
						if (string.IsNullOrEmpty(folderGuid))
						{
							Debug.LogError($"Failed to create folder for character {characterName}");
							return;
						}

						var destinationPath = $"{CHAR_PATH}/{characterName}/{filename}";
						AssetDatabase.MoveAsset(asset, destinationPath);
						AssetDatabase.ImportAsset(destinationPath);
					}
				}
			}
		}
	}
}