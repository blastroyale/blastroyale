using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class Cast3DTo2DMapColliders : AutoPrefabUpdater
	{
		[MenuItem("FLG/Art/Prefab Automation/Physics 3D to 2D/Update Map Colliders")]
		public static void OpenWindow()
		{
			var wnd = GetWindow<Cast3DTo2DMapColliders>();
			wnd.titleContent = new GUIContent("Physics Converter");
		}

		protected override void OnRenderUI()
		{
			GUILayout.Label("Update Map Colliders", EditorStyles.boldLabel);
			GUILayout.Label("Ports all quantum static colliders to 2d static colliders.");
		}
       
		protected override bool OnUpdateGameObject(GameObject o)
		{
			return TryMigrateBox(o) || TryMigrateSphere(o);
		}

		private bool TryMigrateBox(GameObject o)
		{
			var colliders = o.GetComponentsInChildren<QuantumStaticBoxCollider3D>();
			if (colliders == null || colliders.Length == 0) return false;

			foreach (var c in colliders)
			{
				var c2d = c.gameObject.AddComponent<QuantumStaticBoxCollider2D>();
				c2d.SourceCollider = c.SourceCollider;
				c2d.Size = c.Size.XZ;
				c2d.RotationOffset = c.RotationOffset.Y;
				DestroyImmediate(c);
			}

			return true;
		}
       
		private bool TryMigrateSphere(GameObject o)
		{
			var colliders = o.GetComponentsInChildren<QuantumStaticSphereCollider3D>();
			if (colliders == null || colliders.Length == 0) return false;

			foreach (var c in colliders)
			{
				var c2d = c.gameObject.AddComponent<QuantumStaticCircleCollider2D>();
				c2d.SourceCollider = c.SourceCollider;
				c2d.Radius = c.Radius;
				DestroyImmediate(c);
			}
			return true;
		}
	}
}