using System.IO;
using System.Linq;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.Utils;
using Sirenix.Utilities;
using Unity.AssetManager.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEditor.Presets;
using UnityEngine;

namespace FirstLight.Editor.AssetImporters
{
	public class CharacterImporter : AssetPostprocessor
	{
		private const string CHARACTERS_DIR = "Assets/AddressableResources/Collections/CharacterSkins";
		private const string ASSET_MANAGER_PATH = "Assets/Asset Manager";
		private const string CHARACTER_PREFIX = "Char_";
		private const string CHARACTERS_CONFIG = "Assets/AddressableResources/Collections/CharacterSkins/Config.asset";
		private const string ANIMATION_CONTROLLER_PATH = CHARACTERS_DIR + "/Shared/character_animator.controller";

		public override int GetPostprocessOrder() => 100;

		private void OnPreprocessModel()
		{
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
			if (!assetPath.StartsWith(CHARACTERS_DIR)) return;

			// Set defaults for character animation clips.
			var importer = (ModelImporter) assetImporter;

			var clips = importer.defaultClipAnimations;
			foreach (var anim in clips)
			{
				var currentOne = importer.clipAnimations.FirstOrDefault(clip => clip.name == anim.name);
				if (currentOne != null)
				{
					anim.events = currentOne.events;
				}

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
			if (!assetPath.StartsWith(CHARACTERS_DIR)) return;

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
			// TODO: Fix this
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
			if (!Path.GetFileName(assetPath).StartsWith(CHARACTER_PREFIX)) return;

			g.AddComponent<CharacterSkinMonoComponent>().SetupReferences();

			var animator = g.GetComponent<Animator>();
			animator.applyRootMotion = false;

			// This doesn't work here unfortunately
			// animator.runtimeAnimatorController = (AnimatorController) AssetDatabase.LoadMainAssetAtPath(ANIMATION_CONTROLLER_PATH);

			g.SetLayer(LayerMask.NameToLayer("Players"));
		}

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
												   string[] movedFromAssetPaths)
		{
			foreach (var assetPath in importedAssets)
			{
				if (AssetDatabase.IsValidFolder(assetPath) && assetPath.StartsWith(ASSET_MANAGER_PATH))
				{
					// New asset has been imported
					var directoryName = Path.GetFileName(assetPath)!;

					if (directoryName.StartsWith(CHARACTER_PREFIX))
					{
						var characterName = directoryName[CHARACTER_PREFIX.Length..];
						var characterFBXFilename = $"{CHARACTER_PREFIX}{characterName}.fbx";
						var characterFBXPath = Path.Combine(assetPath, characterFBXFilename);
						var characterPrefabFilename = $"{CHARACTER_PREFIX}{characterName}.prefab";
						var characterFBX = (GameObject) AssetDatabase.LoadMainAssetAtPath(characterFBXPath);
						var animations = AssetDatabase.LoadAllAssetRepresentationsAtPath(characterFBXPath).FilterCast<AnimationClip>().ToArray();
						var animatorController = (AnimatorController) AssetDatabase.LoadMainAssetAtPath(ANIMATION_CONTROLLER_PATH);

						Debug.Log($"Importing new character: {characterName}");

						// We need to create a variant because for some reason when we set the animator controller
						// directly on the FBX it loses the reference.
						var characterPrefab = (GameObject) PrefabUtility.InstantiatePrefab(characterFBX);
						var characterAnimator = characterPrefab.GetComponent<Animator>();

						if (animations.Length > 0)
						{
							Debug.Log($"Character {characterName} has custom animations.");
							// If character has custom animations we create an override controller
							var overrideController = new AnimatorOverrideController(animatorController);

							foreach (var clip in animations)
							{
								overrideController[clip.name] = clip;
							}

							AssetDatabase.CreateAsset(overrideController,
								Path.Combine(assetPath, $"{CHARACTER_PREFIX}{characterName}_animator.overrideController"));
							characterAnimator.runtimeAnimatorController = overrideController;
						}
						else
						{
							characterAnimator.runtimeAnimatorController = animatorController;
						}

						// Create prefab variant
						PrefabUtility.SaveAsPrefabAssetAndConnect(characterPrefab, Path.Combine(assetPath, characterPrefabFilename),
							InteractionMode.AutomatedAction, out var success);
						Object.DestroyImmediate(characterPrefab);

						if (!success)
						{
							Debug.LogError($"Error creating prefab for character {characterName}.");
							continue;
						}

						// TODO: If we move the character here it loses the Asset Manager link
						// AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ImportRecursive);
						// MoveCharacters();
					}
				}
			}
		}

		[MenuItem("FLG/Collections/Move Characters")]
		public static void MoveCharacters()
		{
			var folders = AssetDatabase.GetSubFolders(ASSET_MANAGER_PATH);

			foreach (var folder in folders)
			{
				var directoryName = Path.GetFileName(folder);
				var characterName = directoryName[CHARACTER_PREFIX.Length..];

				if (!directoryName.StartsWith(CHARACTER_PREFIX)) continue;

				Debug.Log($"Moving character: {directoryName}");

				var destination = Path.Combine(CHARACTERS_DIR, $"{characterName}");
				var result = AssetDatabase.MoveAsset(folder, destination);
				if (!string.IsNullOrEmpty(result))
				{
					Debug.LogError($"Error moving character {characterName}: {result}");
				}

				AssetDatabase.ImportAsset(destination, ImportAssetOptions.ImportRecursive);
			}
		}
	}
}