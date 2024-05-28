using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	public static class ModelTools
	{
		[MenuItem("FLG/Tools/Run Model Readonly")]
		private static void RunModelReadOnly()
		{
			var modelGuids = AssetDatabase.FindAssets("t:model");  
			var numScenes               = modelGuids.Length;

			AssetDatabase.StartAssetEditing(); 
			Debug.Log("Running Model ReadOnly Tool...");
			var count = 0;
			for (int i = 0; i < numScenes; ++i)
			{
				var sceneAssetPath = AssetDatabase.GUIDToAssetPath(modelGuids[i]);
				var importer = (ModelImporter)UnityEditor.AssetImporter.GetAtPath(sceneAssetPath);
				if (importer.isReadable)
				{
					Debug.Log($"Setting readonly flag {importer.assetPath}]");
					importer.isReadable = false;
					importer.SaveAndReimport();
					count++;
				}
			}
			Debug.Log($"Model ReadOnly flag count {count}");
			Debug.Log("Finished running Model ReadOnly Tool...");
			AssetDatabase.StopAssetEditing();
		}
	}
}