using FirstLight.AssetImporter;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.AssetImporter
{
	/// <inheritdoc />
	[CustomEditor(typeof(AssetConfigsScriptableObject), true)]
	public class AssetConfigsScriptableObjectEditor : Editor 
	{
		/// <inheritdoc />
		public override void OnInspectorGUI()
		{
			var importer = (AssetConfigsScriptableObject) target;
			
			if (GUILayout.Button("Update Path"))
			{
				importer.AssetsFolderPath = GetFilterFolder(EditorUtility.OpenFolderPanel("Select Folder Asset Path", "", ""));
			}
			
			DrawDefaultInspector();
		}

		/// <summary>
		/// Requests the filtered folder path for Asset configs via the given <paramref name="folderPath"/>
		/// </summary>
		public static string GetFilterFolder(string folderPath)
		{
			return folderPath.Substring(folderPath.IndexOf("Assets/"));
		}
	}
}