using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.AssetImporter;
using FirstLight.Game.Configs;
using FirstLight.GoogleSheetImporter;
using FirstLightEditor.GoogleSheetImporter;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace FirstLight.Editor.Configs
{
	/// <summary>
	/// A window to display all of our configs in one place.
	///
	/// TODO: Add more functionality (importing / displaying changes etc...)
	/// </summary>
	public class ConfigsMenuWindow : OdinMenuEditorWindow
	{
		private const string ConfigsFolder = "Assets/AddressableResources/Configs";

		private Dictionary<Type, IGoogleSheetConfigsImporter> _importers = new();

		[MenuItem("FLG/Configs")]
		private static void OpenWindow()
		{
			GetWindow<ConfigsMenuWindow>("Configs").Show();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree(false);
			tree.Config.DrawSearchToolbar = true;

			var gameConfigs = AssetDatabase.LoadAssetAtPath<GameConfigs>($"{ConfigsFolder}/GameConfigs.asset");
			tree.AddObjectAtPath("Game Config",
			                     new QuantumSingleConfigWrapper<GameConfigs, QuantumGameConfig>(gameConfigs));

			tree.AddAllAssetsAtPath("Asset Configs", ConfigsFolder, typeof(AssetConfigsScriptableObject));
			tree.AddAllAssetsAtPath("Settings", $"{ConfigsFolder}/Settings");

			var otherConfigs = AssetDatabase.GetAllAssetPaths()
			                                .Where(x => x.StartsWith(ConfigsFolder) &&
			                                            !x.EndsWith($"{ConfigsFolder}/GameConfigs") &&
			                                            !x.EndsWith("AssetConfigs") &&
			                                            PathUtilities.GetDirectoryName(x).Trim('/') ==
			                                            ConfigsFolder.Trim('/'))
			                                .OrderBy(x => x);

			foreach (var path in otherConfigs)
			{
				if (TryGetWrapper(path, out var wrapper))
				{
					tree.AddObjectAtPath($"Other/{Path.GetFileNameWithoutExtension(path)}", wrapper);
				}
				else
				{
					tree.AddAssetAtPath($"Other/{Path.GetFileNameWithoutExtension(path)}", path);
				}
			}

			_importers = ConfigUtils.GetAllImporters();

			return tree;
		}

		protected override void OnBeginDrawEditors()
		{
			var selected = MenuTree.Selection.FirstOrDefault();
			var toolbarHeight = MenuTree.Config.SearchToolbarHeight;

			SirenixEditorGUI.BeginHorizontalToolbar(toolbarHeight);
			{
				EditorGUILayout.Space();

				var importer = selected == null ? null : GetImporter(selected.Value.GetType());

				if (importer != null && SirenixEditorGUI.ToolbarButton(new GUIContent("Import")))
				{
					var url = importer.GoogleSheetUrl.Replace("edit#", "export?format=csv&");
					var request = UnityWebRequest.Get(url);

					// TODO: Would be nice to disable the Import button while this is running.
					request.SendWebRequest().completed += _ => { ProcessRequest(request, importer); };
				}
			}
			SirenixEditorGUI.EndHorizontalToolbar();
		}

		private void ProcessRequest(UnityWebRequest request, IGoogleSheetConfigsImporter importer)
		{
			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError($"Request failed: {request.error}");
			}

			var values = CsvParser.ConvertCsv(request.downloadHandler.text);

			if (values.Count == 0)
			{
				Debug.Log($"The return sheet was not in CSV format:\n{request.downloadHandler.text}");
			}
			else
			{
				importer.Import(values);
			}

			Debug.Log($"Finished importing google sheet data from {importer.GetType().Name}");
		}

		private IGoogleSheetConfigsImporter GetImporter(Type type)
		{
			var quantumConfigWrapperType = typeof(QuantumSingleConfigWrapper<,>);

			if (type.IsGenericType && type.GetGenericTypeDefinition()
			                              .IsAssignableFrom(quantumConfigWrapperType.GetGenericTypeDefinition()))
			{
				type = type.GetGenericArguments()[0];
			}

			return _importers.TryGetValue(type, out var importer) ? importer : null;
		}

		private bool TryGetWrapper(string path, out object wrapper)
		{
			// TODO: Make this dynamic somehow
			wrapper = Path.GetFileName(path) switch
			{
				"GameModeConfigs.asset" =>
					new QuantumConfigWrapper<GameModeConfigs, QuantumGameModeConfig>(AssetDatabase
						.LoadAssetAtPath<GameModeConfigs>(path)),
				_ => null
			};

			return wrapper != null;
		}

		private class QuantumSingleConfigWrapper<TAsset, TConfig> where TConfig : struct
		                                                          where TAsset : Object, ISingleConfigContainer<TConfig>
		{
			private readonly TAsset _asset;

			[ShowInInspector, HideLabel]
			public TConfig Config
			{
				get => _asset.Config;
				set
				{
					_asset.Config = value;
					EditorUtility.SetDirty(_asset);
				}
			}

			public QuantumSingleConfigWrapper(TAsset asset)
			{
				_asset = asset;
			}
		}

		private class QuantumConfigWrapper<TAsset, TConfig> where TConfig : struct
		                                                    where TAsset : Object, IConfigsContainer<TConfig>
		{
			private readonly TAsset _asset;

			[ShowInInspector, HideLabel]
			public List<TConfig> Configs
			{
				get => _asset.Configs;
				set
				{
					_asset.Configs = value;
					EditorUtility.SetDirty(_asset);
				}
			}

			public QuantumConfigWrapper(TAsset asset)
			{
				_asset = asset;
			}
		}
	}
}