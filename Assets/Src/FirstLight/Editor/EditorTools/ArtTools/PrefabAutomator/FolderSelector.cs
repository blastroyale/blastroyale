using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class FolderSelector
	{
		private static string _path; 

		public string Path => _path;
		
		public bool Valid => _path != null;

		public override string ToString() => _path;
		
		public void OnDrawWidget()
		{
			GUILayout.Label("Folder:");
			_path = EditorGUILayout.TextField(_path);
			if (GUILayout.Button("Choose Folder"))
			{
				string path = EditorUtility.OpenFolderPanel("Select a folder", _path ?? "Assets", "Assets/");
				if (path.Contains(Application.dataPath))
				{
					path = "Assets" + path.Substring(Application.dataPath.Length) + "/";
					_path = path;
				}			
				else Debug.LogError("The path must be in the Assets folder");
			}
		}
	}
}