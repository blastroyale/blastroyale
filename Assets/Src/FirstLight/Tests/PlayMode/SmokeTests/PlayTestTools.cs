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
		public static IEnumerator LoadSceneAndWaitUntilDone(string sceneName)
		{
			var loadSceneAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
			while (!loadSceneAsync.isDone)
			{
				yield return null;
			}
		}
		
		public static IEnumerator UntilChildOfType<T>(GameObject parent) where T : MonoBehaviour
		{
			yield return Until(() => parent.GetComponentInChildren<T>() != null && parent.GetComponentInChildren<T>().gameObject.activeSelf);
		}
		
		public static IEnumerator UntilObjectOfType<T>() where T : MonoBehaviour
		{
			yield return Until(() => Object.FindObjectOfType<T>() != null && Object.FindObjectOfType<T>().gameObject.activeSelf);
		}
		
		public static IEnumerator Until(Func<bool> condition, float timeout = 30f)
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
		
		public static UIDocument GetUIDocument<T>() where T : Component
		{
			var pt = Object.FindObjectOfType<T>();
			if(pt != null) return pt.GetComponent<UIDocument>();
			return null;
		}

		public static void ClickUIToolKitButton<T>(UIDocument parent, string name) where T : VisualElement
		{
			if(parent == null) { throw new NullReferenceException($"UI Document for {name} not found"); }
			var button = parent.rootVisualElement.Q<T>(name);
			
			var buttonPosition = RuntimePanelUtils.ScreenToPanel(parent.rootVisualElement.panel,button.GetPositionOnScreen(parent.rootVisualElement, false));
			using (EventBase mouseDownEvent = MakeMouseEvent(EventType.MouseDown, buttonPosition))
				parent.rootVisualElement.SendEvent(mouseDownEvent);
			using (EventBase mouseUpEvent = MakeMouseEvent(EventType.MouseUp, buttonPosition))
				parent.rootVisualElement.SendEvent(mouseUpEvent);
		}

		public static EventBase MakeMouseEvent(EventType type, Vector2 position, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
		{
			var evt = new Event() { type = type, mousePosition = position, button = (int)button, modifiers = modifiers, clickCount = clickCount};
			if (type ==
				EventType.MouseUp)
				return PointerUpEvent.GetPooled(evt);
			else if (type ==
					 EventType.MouseDown)
				return PointerDownEvent.GetPooled(evt);
			return null;
		}
	}
}