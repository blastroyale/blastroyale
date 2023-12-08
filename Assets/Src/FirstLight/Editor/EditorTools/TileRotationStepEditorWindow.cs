using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// This tool rotates tile transforms at 90 degree random step
	/// found in hierarchy (deep search) - assisting level designers 
	/// </summary>
	public class TileRotationStepEditorWindow : UnityEditor.EditorWindow
	{
		[MenuItem("FLG/Window/TileRotationStepEditorWindow")]
		static void ShowWindow()
		{
			GetWindow<TileRotationStepEditorWindow>("TileRotationStepEditorWindow");
		}
		
		private void OnGUI()
		{
			if (GUILayout.Button("Perform Random Rotation Step"))
			{
				foreach (var go in Selection.gameObjects)
				{
					var transforms = go.GetComponentsInChildren<Transform>();
					foreach (var t in transforms)
					{
						t.localRotation = Quaternion.Euler(0, 90 * UnityEngine.Random.Range(0, 4), 0);
						EditorUtility.SetDirty(t.gameObject);
					}
				}
			}
		}
	}
}