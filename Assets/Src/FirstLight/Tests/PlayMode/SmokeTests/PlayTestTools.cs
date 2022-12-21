using System;
using System.Collections;
using FirstLight.Game.UIElements;
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

		public static void ClickUIToolKitButton(UIDocument parent, string name)
		{
			if(parent == null) { throw new NullReferenceException($"UI Document for {name} not found"); }
			var button = parent.rootVisualElement.Q<Button>(name);
			var navigationSubmitEvent = NavigationSubmitEvent.GetPooled();
			navigationSubmitEvent.target = button;
			button.SendEvent(navigationSubmitEvent);
		}

		public static void ClickUIToolKitImageButton(UIDocument parent, string name)
		{
			if(parent == null) { throw new NullReferenceException($"UI Document for {name} not found"); }
			var button = parent.rootVisualElement.Q<ImageButton>(name);
			button.ClickTest();
		}
	}
}