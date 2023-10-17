using FirstLight.Game.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// 
	/// </summary>
	public class UnityDeactivateColliderEditorWindow : UnityEditor.EditorWindow
	{
		public bool _enable;
		
		[MenuItem("FLG/Window/UnityDeactivateColliderEditorWindow")]
		static void ShowWindow()
		{
			GetWindow<UnityDeactivateColliderEditorWindow>("UnityDeactivateColliderEditorWindow");
		}

		private void OnGUI()
		{
			_enable = EditorGUILayout.Toggle("Enabled", _enable);
			
			if (GUILayout.Button("Deactivate Unity Colliders"))
			{
				var boxColliders = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
				foreach (var c in boxColliders)
				{
					if (c.enabled)
					{
						Debug.Log($"Deactivating {c.gameObject.FullGameObjectPath()} BoxCollider");
						
						c.enabled = _enable;
						EditorUtility.SetDirty(c.gameObject);
					}
				}
				
				var meshColliders = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
				foreach (var c in meshColliders)
				{
					if (c.enabled)
					{
						Debug.Log($"Deactivating {c.gameObject.FullGameObjectPath()} MeshCollider");
						
						c.enabled = _enable;
						EditorUtility.SetDirty(c.gameObject);
					}
				}

				Debug.Log($"Deactivated {boxColliders.Length} BoxColliders");
				Debug.Log($"Deactivated {meshColliders.Length} MeshColliders");
			}
		}
	}
}