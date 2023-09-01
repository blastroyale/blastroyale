using System.Linq;
using FirstLight.Editor.EditorTools.ArtTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class AutoPrefabUpdater : EditorWindow
{
	private bool _updateSceneOnly;
	private FolderSelector _folder = new ();

	protected virtual bool OnValidate() => true;

	protected virtual bool OnUpdateGameObject(GameObject go) => false;
	
	protected virtual void OnRenderUI() {}
	
	void OnGUI()
	{
		OnRenderUI();
		
		_updateSceneOnly = EditorGUILayout.Toggle("Update Scene Only", _updateSceneOnly);
		
		_folder.OnDrawWidget();
		
		if (GUILayout.Button("Replace"))
		{
			if (!OnValidate())
			{
				EditorUtility.DisplayDialog("Yo !", "Invalid selection", "aight");
				return;
			}
			
			if (!_updateSceneOnly && !_folder.Valid)
			{
				EditorUtility.DisplayDialog("Yo !", "Invalid path", "aight");
				return;
			}

			var message = _updateSceneOnly ?  "Update gameObjects in current scene?" : $"Update prefabs in folder {_folder} ?";
			if (EditorUtility.DisplayDialog("Confirm", message, "DOIT", "naw"))
			{
				if (_updateSceneOnly)
				{
					var saveOpenScene = false;
					var l = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

					for (int i = 0; i < l.Count(); i++)
					{
						var o = l.ElementAt(i);
						if (OnUpdateGameObject(o))
						{
							EditorUtility.SetDirty(o);
							saveOpenScene = true;
						}
					}

					if (saveOpenScene)
					{
						var s = EditorSceneManager.GetActiveScene();
						EditorSceneManager.MarkSceneDirty(s);
						EditorSceneManager.SaveScene(s);
					}
				}
				else
				{
					foreach (var prefab in ArtUtils.GetPrefabs(_folder))
					{
						if(OnUpdateGameObject(prefab.LoadedPrefab)) prefab.SaveChanges();
					}
				}
			}
		}
	}
}
