using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// This tool adds QuantumStaticBoxCollider3D component for Unity BoxCollider
	/// found in hierarchy (deep search) - assisting level designers 
	/// </summary>
	public class QuantumBoxColliderEditorWindow : UnityEditor.EditorWindow
	{
		[MenuItem("FLG/Window/QuantumBoxColliderEditor")]
		static void ShowWindow()
		{
			GetWindow<QuantumBoxColliderEditorWindow>("QuantumBoxColliderEditorWindow");
		}

		private void OnGUI()
		{
			if (GUILayout.Button("Add Quantum BoxColliders"))
			{
				foreach (var go in Selection.gameObjects)
				{
					var boxColliders = go.GetComponentsInChildren<BoxCollider>();

					foreach (var b in boxColliders)
					{
						var sb = b.gameObject.AddComponent<QuantumStaticBoxCollider3D>();
						sb.SourceCollider = b;
					}

					if (boxColliders.Length > 0)
					{
						EditorUtility.SetDirty(go);
					}
				}
			}
		}
	}
}