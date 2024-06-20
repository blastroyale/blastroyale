using FirstLight.Game.MonoComponent.Vfx;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// Custom position handles in scene view to set precise spawn location.
	/// </summary>
	[CustomEditor(typeof(VfxSpawnerBase), editorForChildClasses: true)]
	public class VfxSpawnerBaseEditor : UnityEditor.Editor
	{
		private bool _showSpawnPosition = false;

		public override void OnInspectorGUI()
		{
			var newValue = EditorGUILayout.Toggle("Draw Spawn Position In Scene", _showSpawnPosition);
			
			if (_showSpawnPosition != newValue)
			{
				_showSpawnPosition = newValue;
				
				if (_showSpawnPosition)
				{
					Tools.current = Tool.None;
				}
				
				EditorUtility.SetDirty(target);
			}
			
			if (GUILayout.Button("Reset Offset And Rotation"))
			{
				var spawner = (VfxSpawnerBase)target;
				spawner.Offset = Vector3.zero;
				spawner.AdditionalRotation = Vector3.zero;
			}
			
			base.DrawDefaultInspector();
		}

		private void OnSceneGUI()
		{
			if (Tools.current != Tool.None && _showSpawnPosition)
			{
				_showSpawnPosition = false;
				EditorUtility.SetDirty(target);
			}
			
			if (!_showSpawnPosition)
			{
				return;
			}

			EditorGUI.BeginChangeCheck();
			
			var spawner = (VfxSpawnerBase) target;
			var newSpawnPosition =
				Handles.PositionHandle(spawner.SpawnPosition, spawner.SpawnRotation);
			
			if (!EditorGUI.EndChangeCheck())
			{
				return;
			}

			spawner.Offset = newSpawnPosition - spawner.SpawnTargetPosition;
			EditorUtility.SetDirty(target);
		}
	}
}