using FirstLight.Game.MonoComponent.Vfx;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// This tool adds QuantumStaticBoxCollider3D component for Unity BoxCollider
	/// found in hierarchy (deep search) - assisting level designers 
	/// </summary>
	[CustomEditor(typeof(GameObject))]
	public class QuantumBoxColliderEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("Add Quantum BoxColliders"))
			{
				var go = (GameObject)target;

				var boxColliders = go.GetComponentsInChildren<BoxCollider>();

				foreach (var b in boxColliders)
				{
					var sb = b.gameObject.AddComponent<QuantumStaticBoxCollider3D>();
					sb.SourceCollider = b;
				}

				EditorUtility.SetDirty(target);
			}
		}
	}
}