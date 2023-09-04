using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class ShaderUpdater : AutoPrefabUpdater
	{
		public Shader NewShader;
			
		[MenuItem("FLG/Art/Prefab Automation/Replace Shader")]
		public static void OpenWindow()
		{
			MaterialUpdater wnd = GetWindow<MaterialUpdater>();
			wnd.titleContent = new GUIContent("Folder Shader Replace");
		}

		protected override void OnRenderUI()
		{
			GUILayout.Label("Art Shader Replace", EditorStyles.boldLabel);
			NewShader = (Shader)EditorGUILayout.ObjectField(NewShader, typeof(Shader), true);
		}

		protected override bool OnValidate() => NewShader != null;

		protected override bool OnUpdateGameObject(GameObject o)
		{
			var renderers = o.GetComponentsInChildren<Renderer>(true);
			if (renderers == null || renderers.Length == 0) return false;
			
			foreach (var renderer in renderers) 
				renderer.sharedMaterial.shader = NewShader;
			return true;
		}
	}
}