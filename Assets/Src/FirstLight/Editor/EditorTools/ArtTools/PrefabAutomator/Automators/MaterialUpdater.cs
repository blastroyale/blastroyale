using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class MaterialUpdater : AutoPrefabUpdater
	{
		public Material NewMaterial;
			
		[MenuItem("FLG/Art/Prefab Automation/Replace Material")]
		public static void OpenWindow()
		{
			MaterialUpdater wnd = GetWindow<MaterialUpdater>();
			wnd.titleContent = new GUIContent("Folder Material Replace");
		}

		protected override void OnRenderUI()
		{
			GUILayout.Label("Art Material Replace", EditorStyles.boldLabel);
			NewMaterial = (Material)EditorGUILayout.ObjectField(NewMaterial, typeof(Material), true);
		}

		protected override bool OnValidate() => NewMaterial != null;

		protected override bool OnUpdateGameObject(GameObject o)
		{
			var renderers = o.GetComponentsInChildren<Renderer>(true);
			if (renderers == null || renderers.Length == 0) return false;
			
			foreach (var renderer in renderers) renderer.sharedMaterial = NewMaterial;
			return true;
		}
	}
}