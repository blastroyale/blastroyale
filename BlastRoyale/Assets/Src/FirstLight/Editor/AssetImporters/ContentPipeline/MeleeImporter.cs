using System;
using System.IO;
using FirstLight.Game.Configs.Collection;
using FirstLight.Game.MonoComponent.Collections;
using Quantum;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Editor.AssetImporters
{
	public class MeleeImporter : AssetPostprocessor
	{
		private static readonly ContentPipeline _meleeWeaponsPipeline = new ()
		{
			Config = new ()
			{
				Prefix = "Melee_",
				AssetDir = "Assets/AddressableResources/Collections/WeaponSkins/Hammer",
				Preset = "Assets/Presets/MeleeFBX.preset",
				ImportPath = "Assets",
				Layer = "Default"
			},
			OnFinishedImport = RefreshConfig
		};

		[MenuItem("FLG/Collections/Refresh Melee Configs")]
		private static void RefreshConfig()
		{
			var configAsset =
				AssetDatabase.LoadAssetAtPath<WeaponSkinsConfigContainer>("Assets/AddressableResources/Collections/WeaponSkins/Config.asset");
			var config = configAsset.Config;

			config.MeleeWeapons = new ();
			var folders = AssetDatabase.GetSubFolders(_meleeWeaponsPipeline.Config.AssetDir);

			foreach (var folder in folders)
			{
				var directoryName = Path.GetFileName(folder);
				var meleeFileName = $"{_meleeWeaponsPipeline.Config.Prefix}{directoryName}";
				var meleeGameIDName = directoryName == "Hammer" ? "MeleeSkinDefault" : $"MeleeSkin{directoryName}";

				if (!Enum.TryParse<GameId>(meleeGameIDName, out var gameId))
				{
					Log.Warn("Tried to parse game id and failed: "+meleeGameIDName);
				}
				var iconPath = Path.Combine(folder, $"Icon_{meleeFileName}.png");

				config.MeleeWeapons.Add(new Pair<GameId, WeaponSkinConfigEntry>(gameId, new WeaponSkinConfigEntry
				{
					Prefab = new AssetReferenceGameObject(AssetDatabase.GUIDFromAssetPath(Path.Combine(folder, $"{meleeFileName}.prefab"))
						.ToString()),
					Sprite = new AssetReferenceSprite(AssetDatabase.GUIDFromAssetPath(iconPath).ToString())
				}));
			}

			configAsset.Config = config;

			EditorUtility.SetDirty(configAsset);
			AssetDatabase.SaveAssets();
		}

		public override int GetPostprocessOrder() => 100;

		private void OnPreprocessTexture()
		{
			_meleeWeaponsPipeline.OnPreprocessTexture(assetPath, assetImporter);
		}

		private void OnPostprocessModel(GameObject g)
		{
			_meleeWeaponsPipeline.OnPostprocessModel(assetPath, g);
		}

		private void OnPreprocessModel()
		{
			_meleeWeaponsPipeline.OnProcessModel(assetImporter, assetPath);
		}

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
												   string[] movedFromAssetPaths)
		{
			_meleeWeaponsPipeline.OnImportAssets(importedAssets, go =>
			{
				go.AddComponent<WeaponSkinMonoComponent>();
			});
		}
	}
}