using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class MeshColliderUpdater : AutoPrefabUpdater
	{
		public bool CastShadows;
		public bool ReceiveShadows;
			
		[MenuItem("FLG/Art/Prefab Automation/Mesh Collider to Box Collider")]
		public static void OpenWindow()
		{
			var wnd = GetWindow<MeshColliderUpdater>();
			wnd.titleContent = new GUIContent("Mesh Collider Conversion");
		}

		protected override void OnRenderUI()
		{
			GUILayout.Label("Mesh Collider Updater", EditorStyles.boldLabel);
			GUILayout.Label("Converts all mesh colliders to box colliders. Might need manual size adjustments.");
		}
		
		protected override bool OnUpdateGameObject(GameObject o)
		{
			var meshColliders = o.GetComponentsInChildren<MeshCollider>();
			if (meshColliders == null || meshColliders.Length == 0) return false;

			foreach (var c in meshColliders)
			{
				c.gameObject.AddComponent<BoxCollider>();
				DestroyImmediate(c);
			}
			return true;
		}
	}
}