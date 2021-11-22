using FirstLight.UiService;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.UiService
{
	/// <summary>
	/// <see cref="NonDrawingView"/> custom inspector
	/// </summary>
	[CanEditMultipleObjects, CustomEditor(typeof(NonDrawingView), false)]
	public class NonDrawingViewEditor : GraphicEditor
	{
		public override void OnInspectorGUI ()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_Script, new GUILayoutOption[0]);
			
			// skipping AppearanceControlsGUI
			RaycastControlsGUI();
			serializedObject.ApplyModifiedProperties();
		}
	}
}