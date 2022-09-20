using FirstLight.Game.Ids;
using FirstLight.Game.Views;
using FirstLight.UiService;
using UnityEditor;
using UnityEditor.UI;


// ReSharper disable once CheckNamespace

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// Custom editor for ui button class.
	/// </summary>
	[CustomEditor(typeof(UiButtonView), true)]
	public class UiButtonEditor : ButtonEditor
	{
		private SerializedProperty _pressedEase;
		private SerializedProperty _pressedDuration;
		private SerializedProperty _pressedScale;
		private SerializedProperty _anchor;
		private SerializedProperty _clickClip;
		private SerializedProperty _isForward;
		private SerializedProperty _playSound;
		private SerializedProperty _overrideClickSfxAudioId;
		private SerializedProperty _hapticType;
		
		/// <inheritdoc />
		protected override void OnEnable()
		{
			base.OnEnable();
			_pressedEase = serializedObject.FindProperty(nameof(UiButtonView.PressedEase));
			_pressedDuration = serializedObject.FindProperty(nameof(UiButtonView.PressedDuration));
			_pressedScale = serializedObject.FindProperty(nameof(UiButtonView.PressedScale));
			_anchor = serializedObject.FindProperty(nameof(UiButtonView.Anchor));
			_clickClip = serializedObject.FindProperty(nameof(UiButtonView.ClickClip));
			_playSound = serializedObject.FindProperty(nameof(UiButtonView.PlaySound));
			_isForward = serializedObject.FindProperty(nameof(UiButtonView.IsForward));
			_overrideClickSfxAudioId = serializedObject.FindProperty(nameof(UiButtonView.OverrideClickSfxId));
			_hapticType = serializedObject.FindProperty(nameof(UiButtonView.HapticType));
		}
		
		/// <inheritdoc />
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(_pressedEase);
			EditorGUILayout.PropertyField(_pressedDuration);
			EditorGUILayout.PropertyField(_pressedScale);
			EditorGUILayout.PropertyField(_anchor);
			EditorGUILayout.PropertyField(_clickClip);
			EditorGUILayout.PropertyField(_playSound);
			EditorGUILayout.PropertyField(_isForward);
			EditorGUILayout.PropertyField(_overrideClickSfxAudioId);
			EditorGUILayout.PropertyField(_hapticType);
			serializedObject.ApplyModifiedProperties();
		}
	}
}