using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Editor.EditorTools.ArtTools;
using UnityEditor;
using UnityEngine;

public class AutoPrefabUpdater : EditorWindow
{
	private FolderSelector _folder = new ();

	protected virtual bool OnValidate() => true;

	protected virtual bool OnUpdatePrefab(GameObject prefab) => false;

	protected virtual void OnRenderUI() {}
	
	void OnGUI()
	{
		OnRenderUI();
		_folder.OnDrawWidget();
	
		if (GUILayout.Button("Replace"))
		{
			if (!OnValidate())
			{
				EditorUtility.DisplayDialog("Yo !", "Invalid selection", "aight");
				return;
			}
			
			if (!_folder.Valid)
			{
				EditorUtility.DisplayDialog("Yo !", "Invalid path", "aight");
				return;
			}
			
			if (EditorUtility.DisplayDialog("Confirm", $"Update prefabs in folder {_folder} ?", "DOIT", "naw"))
			{
				foreach (var prefab in ArtUtils.GetPrefabs(_folder))
				{
					if(OnUpdatePrefab(prefab.LoadedPrefab)) prefab.SaveChanges();
				}
			}
		}
	}
}
