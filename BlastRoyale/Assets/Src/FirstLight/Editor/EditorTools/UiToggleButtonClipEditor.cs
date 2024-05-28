using FirstLight.UiService;
using UnityEditor;
using UnityEditor.UI;

// ReSharper disable once CheckNamespace

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// Custom editor for ui toggle button class.
	/// </summary>
	[CustomEditor(typeof(UiToggleButtonClipView), true)]
	public class UiToggleButtonClipEditor : UiToggleButtonEditor
	{
		private SerializedProperty _clip;

		/// <inheritdoc />
		protected override void OnEnable()
		{
			base.OnEnable();
			_clip = serializedObject.FindProperty(nameof(UiToggleButtonClipView.Clip));
		}
		
		/// <inheritdoc />
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			serializedObject.Update();
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(_clip);
			serializedObject.ApplyModifiedProperties();
		}
	}
}