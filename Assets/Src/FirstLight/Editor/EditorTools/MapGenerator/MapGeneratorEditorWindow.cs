using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Editor.AssetImporters;
using FirstLight.Editor.SheetImporters;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLightEditor.AssetImporter;
using FirstLightEditor.GoogleSheetImporter;
using Quantum;
using Quantum.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Debug = UnityEngine.Debug;


namespace FirstLight.Editor.EditorTools.MapGenerator
{
	/// <summary>
	/// This editor window provides functionality for generating a map 
	/// </summary>
	public class MapGeneratorEditorWindow : OdinEditorWindow
	{
		public struct AllowedMapStruct
		{
			public string Id;
			public bool AllowedMap;
		}
		
		private const string _exportFolderPath = "Assets/AddressableResources/Scenes";
		public GameId _mapGameId;
		public List<AllowedMapStruct> _allowedMaps = new List<AllowedMapStruct>();
		
		public int _maxPlayers = 20;
		public bool _isTestMap = true;
		public bool _isCustomOnly = false;
		public float _dropSelectionSize = 1f;
		private const string _sceneIdScriptPath = "Src/FirstLight/Game/Ids/SceneId.cs";
		private AssetReference _assetReference;

		protected override void Initialize()
		{
			if (_allowedMaps.Count > 0)
			{
				return;
			}

			var gameModeConfigs = AssetDatabase.LoadAssetAtPath<GameModeConfigs>("Assets/AddressableResources/Configs/GameModeConfigs.asset");
			if (gameModeConfigs == null)
			{
				Debug.LogError("Game Mode Configs reference invalid");
				
				return;
			}

			for (int i = 0; i < gameModeConfigs.Configs.Count; i++)
			{
				_allowedMaps.Add(new AllowedMapStruct()
				{
					Id = gameModeConfigs.Configs[i].Id,
					AllowedMap = false
				});
			}
		}
		
		[MenuItem("FLG/Generators/Map Generator EditorWindow")]
		private static void OpenWindow()
		{
			GetWindow<MapGeneratorEditorWindow>("Map Generator EditorWindow").Show();
		}
        
		
		
		[Button("Export Scene")]
		private void ExportScene()
		{
			if (_mapGameId == GameId.Random)
			{
				Debug.LogError($"Invalid map game id [{_mapGameId}]");
				
				return;
			}

			var sceneDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), _exportFolderPath);
			
			if (!Directory.Exists(sceneDirectoryPath))
			{
				Debug.LogError($"Invalid export folder path [{sceneDirectoryPath}]");

				return;
			}

			
			var scenePath = Path.Combine(sceneDirectoryPath, _mapGameId.ToString() + ".unity");
			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
			
			var quantumMap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/Environment/Root/QuantumMap.prefab");
			var sceneIdPath = Path.Combine(Application.dataPath, _sceneIdScriptPath);

			if (sceneIdPath == "" || !File.Exists(sceneIdPath))
			{
				Debug.LogError($"Invalid scene id path [{sceneIdPath}]");

				return;
			}
			
			string fileContents = File.ReadAllText(sceneIdPath);
			string modifiedContents = AddEnumValue(fileContents, "SceneId", _mapGameId.ToString());

			if (string.IsNullOrEmpty(modifiedContents))
			{
				File.WriteAllText(sceneIdPath, modifiedContents);
			}

			var settings = AddressableAssetSettingsDefaultObject.Settings;
			var group = settings.DefaultGroup;
			var guid = AssetDatabase.GUIDFromAssetPath($"Assets/AddressableResources/Scenes/{_mapGameId.ToString()}.unity");

			var entry = settings.CreateOrMoveEntry(guid.ToString(), group, readOnly: false, postEvent: true);
			entry.address = AssetDatabase.GUIDToAssetPath(guid);
			
			_assetReference = new AssetReference(guid.ToString());
			
			var go = Instantiate(quantumMap, Vector3.zero, Quaternion.identity);
			
			MapAsset asset = ScriptableObject.CreateInstance<MapAsset>();
			
			// Create a path for the new asset
			string path = $"Assets/AddressableResources/Maps/{_mapGameId}.asset";
			
			// Save the asset at the specified path
			AssetDatabase.CreateAsset(asset, path);
			
			var mapData = go.GetComponent<MapData>();
			mapData.Asset = asset;
			
			QuantumAutoBaker.BakeMap(mapData, mapData.BakeAllMode);
			
			asset.Settings.UserAsset = asset.AssetObject;
			asset.Settings.Scene = _mapGameId.ToString();
			asset.Settings.ScenePath = Path.Combine(_exportFolderPath, $"{_mapGameId}.unity");
			asset.Settings.SceneGuid = guid.ToString();
			
			EditorUtility.SetDirty(asset);
			
			EditorSceneManager.SaveScene(scene, scenePath);
			EditorSceneManager.CloseScene(scene, true);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			
			Debug.Log("Finished scene export");
		}

		[Button("Generate Asset Configs")]
		private void GenerateAssetConfigs()
		{
			var sceneAssetConfigs = AssetDatabase.LoadAssetAtPath<SceneAssetConfigs>("Assets/AddressableResources/Configs/SceneAssetConfigs.asset");
			if (sceneAssetConfigs == null)
			{
				Debug.LogError("Scene Asset Configs reference invalid");
				
				return;
			}
			
			if (Enum.TryParse<SceneId>(_mapGameId.ToString(), out var sceneId))
			{
				for (int i = 0; i < sceneAssetConfigs.Configs.Count(); i++)
				{
					if (sceneAssetConfigs.Configs[i].Key == sceneId)
					{
						sceneAssetConfigs.Configs.RemoveAt(i);
						break;
					}
				}
				sceneAssetConfigs.Configs.Add(new Pair<SceneId, AssetReference>(sceneId, _assetReference));
				
				EditorUtility.SetDirty(sceneAssetConfigs);
				
				var importers = AssetsToolImporter.GetAllImporters();
				var importData = importers.FirstOrDefault(e => e.Type == typeof(SceneAssetConfigsImporter));
				
				importData.Importer.Import();
				
				GenerateMapAssetConfigs();
				
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				
				Debug.Log("Finished generating scene asset configs");
			}
			else
			{
				Debug.LogError($"Cannot parse SceneId {_mapGameId}");
			}
		}
		
		private void GenerateMapAssetConfigs()
		{
			var mapConfigs = AssetDatabase.LoadAssetAtPath<MapConfigs>("Assets/AddressableResources/Configs/MapConfigs.asset");
			if (mapConfigs == null)
			{
				Debug.LogError("Map Configs reference invalid");
				
				return;
			}
			
			var gameModeConfigs = AssetDatabase.LoadAssetAtPath<GameModeConfigs>("Assets/AddressableResources/Configs/GameModeConfigs.asset");
			if (gameModeConfigs == null)
			{
				Debug.LogError("Game Mode Configs reference invalid");
				
				return;
			}
			
			if (mapConfigs.Configs.Any(c => c.Map == _mapGameId))
			{
				var configs = mapConfigs.Configs.Where(c => c.Map == _mapGameId).ToList();

				for (int i = 0; i < configs.Count(); i++)
				{
					mapConfigs.Configs.Remove(configs[i]);
				}
			}
			
			mapConfigs.Configs.Add(new QuantumMapConfig()
			{
				Map = _mapGameId,
				MaxPlayers = _maxPlayers,
				IsTestMap = _isTestMap,
				IsCustomOnly = _isCustomOnly,
				DropSelectionSize = _dropSelectionSize
			});
			
			EditorUtility.SetDirty(mapConfigs);

			for (int i = 0; i < gameModeConfigs.Configs.Count(); i++)
			{
				gameModeConfigs.Configs[i].AllowedMaps.Remove(_mapGameId);
			}

			foreach (var m in _allowedMaps)
			{
				for (int i = 0; i < gameModeConfigs.Configs.Count(); i++)
				{
					if (m.AllowedMap && m.Id == gameModeConfigs.Configs[i].Id)
					{
						gameModeConfigs.Configs[i].AllowedMaps.Add(_mapGameId);
					}
				}
			}
			
			EditorUtility.SetDirty(gameModeConfigs);
			AssetDatabase.SaveAssets();
			
			Debug.Log("Finished generating map asset configs");
		}

		
		private static string AddEnumValue(string fileContents, string enumName, string enumValue)
		{
			int enumEntryStartIndex = fileContents.IndexOf(enumValue);
			if (enumEntryStartIndex != -1)
			{
				return null;
			}
			
			// Find the enum declaration
			string enumDeclaration = $"enum {enumName}";

			int enumStartIndex = fileContents.IndexOf(enumDeclaration);
			if (enumStartIndex == -1)
			{
				throw new InvalidOperationException($"Enum '{enumName}' not found in the file.");
			}

			// Find the closing brace of the enum declaration
			int enumEndIndex = fileContents.IndexOf('}', enumStartIndex);
			if (enumEndIndex == -1)
			{
				throw new InvalidOperationException($"Closing brace of enum '{enumName}' not found in the file.");
			}

			// Find the last enum value
			int lastEnumValueIndex = fileContents.LastIndexOf(',', enumEndIndex);
			if (lastEnumValueIndex == -1)
			{
				lastEnumValueIndex = enumEndIndex - 1;
			}

			// Insert a comma if it doesn't exist
			if (fileContents[lastEnumValueIndex] != ',')
			{
				fileContents = fileContents.Insert(lastEnumValueIndex + 1, ",");
			}

			// Insert the new enum value before the closing brace
			string newEnumValue = $"\t{enumValue},\n\t";

			string modifiedContents = fileContents.Insert(enumEndIndex, newEnumValue);

			return modifiedContents;
		}
	}
}



