using System;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor
{
	internal class DelayedEditorCall
	{
		private readonly Action _action;
		private readonly float _executeTime;

		private DelayedEditorCall(Action action, float delay)
		{
			_action = action;
			_executeTime = Time.realtimeSinceStartup + delay;
		}

		private void Run()
		{
			if (Time.realtimeSinceStartup < _executeTime) return;

			_action();

			EditorApplication.update -= Run;
		}

		public static void DelayedCall(Action action, float delay)
		{
			var obj = new DelayedEditorCall(action, delay);
			EditorApplication.update += obj.Run;
		}
	}
}