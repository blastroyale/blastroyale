using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class QuantumColliderUpdater : AutoPrefabUpdater
	{
		public bool CastShadows;
		public bool ReceiveShadows;
			
		[MenuItem("FLG/Art/Prefab Automation/Quantum Collider Setup")]
		public static void OpenWindow()
		{
			var wnd = GetWindow<QuantumColliderUpdater>();
			wnd.titleContent = new GUIContent("Quantum Collider Updater");
		}

		protected override void OnRenderUI()
		{
			GUILayout.Label("Quantum Collider Updater", EditorStyles.boldLabel);
			GUILayout.Label("Ensures all objects that have box colliders to have a quantum static collider.");
			GUILayout.Label("Will skip objects that already have quantum static colliders.");
		}
		
		protected override bool OnUpdatePrefab(GameObject o)
		{
			var colliders = o.GetComponentsInChildren<BoxCollider>();
			if (colliders == null || colliders.Length == 0) return false;

			foreach (var c in colliders)
			{
				var box = c.gameObject.GetComponent<QuantumStaticBoxCollider3D>();
				if (box != null)
				{
					if (box.SourceCollider != null) continue;
					else box.SourceCollider = c;
				}
				else
				{
					box = c.gameObject.AddComponent<QuantumStaticBoxCollider3D>();
					box.SourceCollider = c;
				}
			}
			return true;
		}
	}
}