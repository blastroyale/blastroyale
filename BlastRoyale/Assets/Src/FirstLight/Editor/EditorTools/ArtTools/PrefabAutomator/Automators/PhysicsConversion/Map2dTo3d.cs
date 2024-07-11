using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class Cast2DTo3DMapColliders : AutoPrefabUpdater
	{
		[MenuItem("FLG/Art/Prefab Automation/Physics 2D to 3D/Update Map Colliders")]
		public static void OpenWindow()
		{
			var wnd = GetWindow<Cast2DTo3DMapColliders>();
			wnd.titleContent = new GUIContent("Physics Converter");
		}

		protected override void OnRenderUI()
		{
			GUILayout.Label("Update Map Colliders", EditorStyles.boldLabel);
			GUILayout.Label("Ports all quantum static colliders to 3d static colliders.");
		}
       
		protected override bool OnUpdateGameObject(GameObject o)
		{
			return TryMigrateBox(o) || TryMigrateSphere(o);
		}

		private bool TryMigrateBox(GameObject o)
		{
			var colliders = o.GetComponentsInChildren<QuantumStaticBoxCollider2D>();
			if (colliders == null || colliders.Length == 0) return false;

			foreach (var c in colliders)
			{
				var c2d = c.gameObject.AddComponent<QuantumStaticBoxCollider3D>();
				c2d.Size = c.Size.XOY;
				c2d.Size.Y = 2;
				DestroyImmediate(c);
			}

			return true;
		}
       
		private bool TryMigrateSphere(GameObject o)
		{
			var colliders = o.GetComponentsInChildren<QuantumStaticCircleCollider2D>();
			if (colliders == null || colliders.Length == 0) return false;

			foreach (var c in colliders)
			{
				var c2d = c.gameObject.AddComponent<QuantumStaticSphereCollider3D>();
				c2d.Radius = c.Radius;
				DestroyImmediate(c);
			}
			return true;
		}
	}
}