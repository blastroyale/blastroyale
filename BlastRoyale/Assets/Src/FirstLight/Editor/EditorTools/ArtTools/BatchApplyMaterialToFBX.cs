namespace FirstLight.Editor.EditorTools.ArtTools
{
	using UnityEditor;
	using UnityEngine;

	public class BatchApplyMaterialToFBX : EditorWindow
	{
		private Material materialToApply;

		[MenuItem("FLG/Art/Batch Apply Material to FBX")]
		public static void ShowWindow()
		{
			GetWindow<BatchApplyMaterialToFBX>("Batch Apply Material to FBX");
		}

		private void OnGUI()
		{
			GUILayout.Label("Batch Apply Material to FBX", EditorStyles.boldLabel);

			materialToApply = (Material) EditorGUILayout.ObjectField("Material", materialToApply, typeof(Material), false);

			if (GUILayout.Button("Apply Material to Selected FBX"))
			{
				ApplyMaterialToSelectedFBX();
			}

			if (GUILayout.Button("Apply Material to Folder FBX"))
			{
				ApplyMaterialToFBXInFolder();
			}
		}

		private void ApplyMaterialToSelectedFBX()
		{
			if (materialToApply == null)
			{
				Debug.LogError("Please select a material to apply.");
				return;
			}

			Object[] selectedObjects = Selection.objects;
			foreach (var obj in selectedObjects)
			{
				string assetPath = AssetDatabase.GetAssetPath(obj);
				if (assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
				{
					ApplyMaterialToFBX(assetPath);
				}
			}

			Debug.Log("Material applied to selected FBX files.");
		}

		private void ApplyMaterialToFBXInFolder()
		{
			if (materialToApply == null)
			{
				Debug.LogError("Please select a material to apply.");
				return;
			}

			string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");

			if (string.IsNullOrEmpty(folderPath))
			{
				return;
			}

			string relativePath = "Assets" + folderPath.Replace(Application.dataPath, "");
			string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] {relativePath});

			foreach (string guid in fbxGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if (assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
				{
					ApplyMaterialToFBX(assetPath);
				}
			}

			Debug.Log("Material applied to FBX files in folder.");
		}

		private void ApplyMaterialToFBX(string assetPath)
		{
			GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

			if (fbx == null)
			{
				Debug.LogWarning($"Failed to load FBX: {assetPath}");
				return;
			}

			Renderer[] renderers = fbx.GetComponentsInChildren<Renderer>();

			foreach (Renderer renderer in renderers)
			{
				Undo.RecordObject(renderer, "Batch Apply Material");
				renderer.sharedMaterial = materialToApply;
				EditorUtility.SetDirty(renderer);
			}
		}
	}
}