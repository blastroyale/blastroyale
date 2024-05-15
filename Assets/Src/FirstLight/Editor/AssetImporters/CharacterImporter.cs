using System;
using System.IO;
using System.Linq;
using FirstLight.Editor.EditorTools;
using FirstLight.Editor.Ids;
using FirstLight.Game.Configs;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEditor.Compilation;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using LayerMask = UnityEngine.LayerMask;
using Object = UnityEngine.Object;

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
			if (!Path.GetFileName(assetPath).StartsWith(CHARACTER_PREFIX)) return;

			var importer = (ModelImporter) assetImporter;

			// Extract textures
			var folder = assetPath!.Remove(assetPath.LastIndexOf('/'));
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
			if (!Path.GetFileName(assetPath).StartsWith(CHARACTER_PREFIX)) return;

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
			var charactersAdded = false;

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

						charactersAdded = true;
					}
				}
			}

			// TODO: If we move the character directly here it loses the Asset Manager link, this is a workaround
			if (charactersAdded)
			{
				DelayedEditorCall.DelayedCall(() =>
				{
					MoveCharacters();
					RefreshConfigs();
				}, 0.5f);
			}
		}

		private static void MoveCharacters()
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

		[MenuItem("FLG/Collections/Refresh Character Configs")]
		private static void RefreshConfigs()
		{
			var configAsset = AssetDatabase.LoadAssetAtPath<CharacterSkinConfigs>(CHARACTERS_CONFIG);
			var config = configAsset.Config;
			var gameIdCandidates = GameIDGenerator.GenerateNewGameIDs();

			config.Skins.Clear();

			var folders = AssetDatabase.GetSubFolders(CHARACTERS_DIR);
			var idsGenerated = false;
			foreach (var folder in folders)
			{
				var directoryName = Path.GetFileName(folder);

				if (directoryName == "Shared") continue; // Ignore shared folder

				Debug.Log($"Adding character to config: {directoryName}");

				var characterFileName = $"{CHARACTER_PREFIX}{directoryName}";

				var gameId = NameToGameId(directoryName);

				if (!gameId.HasValue)
				{
					Debug.LogWarning("Missing GameID for character: " + directoryName);
					var createGameID = EditorUtility.DisplayDialog("Missing GameID!",
						$"No GameID was found for the character {directoryName}. Would you like to create one?\n", "DA", "NYET");

					if (createGameID)
					{
						GameIDGenerator.AddNewCharacterGameID(directoryName, gameIdCandidates);
						idsGenerated = true;
					}
				}

				config.Skins.Add(new CharacterSkinConfigEntry
				{
					GameId = gameId ?? GameId.Random,
					Prefab = new AssetReferenceGameObject(AssetDatabase.GUIDFromAssetPath(Path.Combine(folder, $"{characterFileName}.prefab"))
						.ToString()),
					Sprite = new AssetReferenceSprite(AssetDatabase.GUIDFromAssetPath(Path.Combine(folder, $"Icon_{characterFileName}.png"))
						.ToString()),
				});
			}

			if (idsGenerated)
			{
				EditorUtility.DisplayDialog("Rebuild required.",
					"New GameIDs have been generated. Please regenerate the QTN files, rebuild Quantum, and run FLG/Collections/Refresh Character Configs.",
					"Ajde");
				CompilationPipeline.RequestScriptCompilation();
			}

			configAsset.Config = config;
			EditorUtility.SetDirty(configAsset);
			AssetDatabase.SaveAssetIfDirty(configAsset);
		}

		private static GameId? NameToGameId(string name)
		{
			if (Enum.TryParse(typeof(GameId), $"PlayerSkin{name}", true, out var gid))
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