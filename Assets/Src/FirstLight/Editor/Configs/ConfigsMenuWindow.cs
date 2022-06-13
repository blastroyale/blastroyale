using System.IO;
using System.Linq;
using FirstLight;
using FirstLight.AssetImporter;
using FirstLight.Game.Configs;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;

namespace FirstLightEditor.AssetImporter
{
	/// <summary>
	/// A window to display all of our configs in one place.
	///
	/// TODO: Add more functionality (importing / displaying changes etc...)
	/// </summary>
	public class ConfigsMenuWindow : OdinMenuEditorWindow
	{
		private const string ConfigsFolder = "Assets/AddressableResources/Configs";

		[MenuItem("First Light Games/Configs")]
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

			return tree;
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