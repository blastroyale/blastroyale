using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.AssetImporter
{
	/// <summary>
	/// Scriptable Object tool to import all or specific assets data
	/// </summary>
	[CreateAssetMenu(fileName = "AssetsImporter", menuName = "ScriptableObjects/Editor/AssetsImporter")]
	public class AssetsImporter : ScriptableObject
	{
		[MenuItem("Tools/Assets Importer/Select AssetsImporter.asset")]
		private static void SelectAssetsImporter()
		{
			var assets = AssetDatabase.FindAssets($"t:{nameof(AssetsImporter)}");
			var scriptableObject = assets.Length > 0 ? 
				                       AssetDatabase.LoadAssetAtPath<AssetsImporter>(AssetDatabase.GUIDToAssetPath(assets[0])) :
				                       CreateInstance<AssetsImporter>();

			if (assets.Length == 0)
			{
				AssetDatabase.CreateAsset(scriptableObject, $"Assets/{nameof(AssetsImporter)}.asset");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			Selection.activeObject = scriptableObject;
		}
	}
}