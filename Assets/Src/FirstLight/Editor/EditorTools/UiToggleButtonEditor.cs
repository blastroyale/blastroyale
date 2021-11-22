using FirstLight.UiService;
using UnityEditor;
using UnityEditor.UI;

// ReSharper disable once CheckNamespace

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// Custom editor for ui toggle button class.
	/// </summary>
	[CustomEditor(typeof(UiToggleButtonView), true)]
	public class UiToggleButtonEditor : ToggleEditor
	{
		private SerializedProperty _toggleOn;
		private SerializedProperty _toggleOff;
		private SerializedProperty _toggleOnPressedClip;
		private SerializedProperty _toggleOffPressedClip;
		private SerializedProperty _pressedEase;
		private SerializedProperty _pressedDuration;
		private SerializedProperty _pressedScale;
		private SerializedProperty _anchor;
		
		/// <inheritdoc />
		protected override void OnEnable()
		{
			base.OnEnable();
			_toggleOn = serializedObject.FindProperty(nameof(UiToggleButtonView.ToggleOn));
			_toggleOff = serializedObject.FindProperty(nameof(UiToggleButtonView.ToggleOff));
			_toggleOnPressedClip = serializedObject.FindProperty(nameof(UiToggleButtonView.ToggleOnPressedClip));
			_toggleOffPressedClip = serializedObject.FindProperty(nameof(UiToggleButtonView.ToggleOffPressedClip));
			_pressedEase = serializedObject.FindProperty(nameof(UiToggleButtonView.PressedEase));
			_pressedDuration = serializedObject.FindProperty(nameof(UiToggleButtonView.PressedDuration));
			_pressedScale = serializedObject.FindProperty(nameof(UiToggleButtonView.PressedScale));
			_anchor = serializedObject.FindProperty(nameof(UiToggleButtonView.Anchor));
		}
		
		/// <inheritdoc />
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(_toggleOn);
			EditorGUILayout.PropertyField(_toggleOff);
			EditorGUILayout.PropertyField(_toggleOnPressedClip);
			EditorGUILayout.PropertyField(_toggleOffPressedClip);
			EditorGUILayout.PropertyField(_pressedEase);
			EditorGUILayout.PropertyField(_pressedDuration);
			EditorGUILayout.PropertyField(_pressedScale);
			EditorGUILayout.PropertyField(_anchor);
			serializedObject.ApplyModifiedProperties();
		}
	}
}