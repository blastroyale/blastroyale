using System;
using System.Collections;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FirstLight.Tests.PlayTests
{
	public class TestTools
	{
		/// <summary>
		/// Coroutine to load a scene and wait until its done loading
		/// </summary>
		/// <param name="sceneName">Name of the scene</param>
		public static IEnumerator LoadSceneAndWaitUntilDone(string sceneName)
		{
			var loadSceneAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
			while (!loadSceneAsync.isDone)
			{
				yield return null;
			}
		}
		
		/// <summary>
		/// Coroutine that waits until an object of a certain type is in the scene
		/// </summary>
		/// <typeparam name="T">Type of the object to wait for</typeparam>
		public static IEnumerator UntilObjectOfType<T>() where T : MonoBehaviour
		{
			yield return Until(() => Object.FindObjectOfType<T>() != null && Object.FindObjectOfType<T>().gameObject.activeSelf);
		}

		/// <summary>
		/// Find a certain UIDocument in the scene
		/// </summary>
		/// <typeparam name="T">Type of UIDocument to find</typeparam>
		/// <returns></returns>
		public static UIDocument GetUIDocument<T>() where T : Component
		{
			var pt = Object.FindObjectOfType<T>();
			if(pt != null) return pt.GetComponent<UIDocument>();
			return null;
		}

		/// <summary>
		/// Simulate a click on a button in a certain document
		/// </summary>
		/// <param name="parent">Document the button is in</param>
		/// <param name="name">Name of the button</param>
		/// <typeparam name="T">Type of button (ex: Button, ImageButton, etc)</typeparam>
		public static void ClickUIToolKitButton<T>(UIDocument parent, string name) where T : VisualElement
		{
			if(parent == null) { throw new NullReferenceException($"UI Document for {name} not found"); }
			var button = parent.rootVisualElement.Q<T>(name);

			if (button == null)
			{
				throw new Exception($"Button with name \"{name}\" not found");
			}
			
			var buttonPosition = RuntimePanelUtils.ScreenToPanel(parent.rootVisualElement.panel,button.GetPositionOnScreen(parent.rootVisualElement, false));
			using (EventBase mouseDownEvent = MakeMouseEvent(EventType.MouseDown, buttonPosition))
				parent.rootVisualElement.SendEvent(mouseDownEvent);
			using (EventBase mouseUpEvent = MakeMouseEvent(EventType.MouseUp, buttonPosition))
				parent.rootVisualElement.SendEvent(mouseUpEvent);
		}

		private static EventBase MakeMouseEvent(EventType type, Vector2 position, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
		{
			var evt = new Event() { type = type, mousePosition = position, button = (int)button, modifiers = modifiers, clickCount = clickCount};
			
			EventBase returnEvent = type switch
			{
				EventType.MouseUp   => PointerUpEvent.GetPooled(evt),
				EventType.MouseDown => PointerDownEvent.GetPooled(evt),
				_ => null
			};

			return returnEvent;
		}
		
		private static IEnumerator Until(Func<bool> condition, float timeout = 30f)
		{
			float timePassed = 0f;
			var wait = new WaitForSeconds(1);
			while (!condition() && timePassed < timeout)
			{
				yield return wait;
				timePassed += Time.deltaTime;
			}
			if (timePassed >= timeout) {
				throw new TimeoutException("Condition was not fulfilled for " + timeout + " seconds.");
			}
		}
	}
}