using System;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Input
{
	public static class GameInputHelper
	{
		/// <summary>
		/// Adds all type of listeners for the given input.
		/// Temporary class - will be removed soon to only use "performed"
		/// </summary>
		public static void AddListener(this InputAction action, Action<InputAction.CallbackContext> callback)
		{
			action.performed += callback;
			action.started += callback;
			action.canceled += callback;
		}

		public static void RemoveListener(this InputAction action, Action<InputAction.CallbackContext> callback)
		{
			action.performed -= callback;
			action.started -= callback;
			action.canceled -= callback;
		}
	}
}