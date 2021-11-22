using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.GoogleSheetImporter
{
	/// <summary>
	/// Scriptable Object tool to import all or specific google sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GoogleSheetImporter", menuName = "ScriptableObjects/Editor/GoogleSheetImporter")]
	public class GoogleSheetImporter : ScriptableObject
	{
		public string ReplaceSpreadsheetId;
		
		[MenuItem("Tools/GoogleSheet Importer/Select GoogleSheetImporter.asset")]
		private static void SelectSheetImporter()
		{
			var assets = AssetDatabase.FindAssets($"t:{nameof(GoogleSheetImporter)}");
			var scriptableObject = assets.Length > 0 ? 
				                       AssetDatabase.LoadAssetAtPath<GoogleSheetImporter>(AssetDatabase.GUIDToAssetPath(assets[0])) :
				                       CreateInstance<GoogleSheetImporter>();

			if (assets.Length == 0)
			{
				AssetDatabase.CreateAsset(scriptableObject, $"Assets/{nameof(GoogleSheetImporter)}.asset");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			Selection.activeObject = scriptableObject;
		}
	}
}