using System;
using System.Collections;
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
			while (!condition() && timePassed < timeout) {
				yield return new WaitForSeconds(1);
				timePassed += Time.deltaTime;
			}
			if (timePassed >= timeout) {
				throw new TimeoutException("Condition was not fulfilled for " + timeout + " seconds.");
			}
		}
		
		public static void ClickUIToolKitButton(UIDocument parent, string name)
		{
			var button = parent.rootVisualElement.Q<Button>(name);

			var navigationSubmitEvent = NavigationSubmitEvent.GetPooled();
			navigationSubmitEvent.target = button;
			button.SendEvent(navigationSubmitEvent);
		}
	}
}