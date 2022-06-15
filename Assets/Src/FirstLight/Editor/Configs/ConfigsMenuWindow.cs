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
			tree.DrawSearchToolbar();

			var gameConfigs = AssetDatabase.LoadAssetAtPath<GameConfigs>($"{ConfigsFolder}/GameConfigs.asset");
			tree.AddObjectAtPath("Game Config", new QuantumConfigWrapper<GameConfigs, QuantumGameConfig>(gameConfigs));

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
				tree.AddAssetAtPath($"Other/{Path.GetFileNameWithoutExtension(path)}", path);
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

				if (importer != null)
				{
					if (SirenixEditorGUI.ToolbarButton(new GUIContent("Import")))
					{
						var url = importer.GoogleSheetUrl.Replace("edit#", "export?format=csv&");
						var request = UnityWebRequest.Get(url);

						// TODO: Would be nice to disable the Import button while this is running.
						request.SendWebRequest().completed += d =>
						{
							if (request.result != UnityWebRequest.Result.Success)
							{
								throw new Exception(request.error);
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

							Debug.Log($"Finished importing google sheet data from {selected.Value.GetType().Name}");
						};
					}
				}
			}
			SirenixEditorGUI.EndHorizontalToolbar();
		}

		private IGoogleSheetConfigsImporter GetImporter(Type type)
		{
			var quantumConfigWrapperType = typeof(QuantumConfigWrapper<,>);

			if (type.IsGenericType && type.GetGenericTypeDefinition()
			                              .IsAssignableFrom(quantumConfigWrapperType.GetGenericTypeDefinition()))
			{
				type = type.GetGenericArguments()[0];
			}

			return _importers.TryGetValue(type, out var importer) ? importer : null;
		}


		private class QuantumConfigWrapper<TAsset, TConfig> where TConfig : struct
		                                                    where TAsset : ISingleConfigContainer<TConfig>
		{
			private TAsset Asset;

			[ShowInInspector, HideLabel]
			public TConfig Config
			{
				get => Asset.Config;
				set => Asset.Config = value;
			}

			public QuantumConfigWrapper(TAsset asset)
			{
				Asset = asset;
			}
		}
	}
}