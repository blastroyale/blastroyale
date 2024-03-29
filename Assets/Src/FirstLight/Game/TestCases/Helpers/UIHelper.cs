using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace FirstLight.Game.TestCases.Helpers
{
	public class UIHelper : TestHelper
	{
		public IEnumerator WaitForPresenter<T>(float waitAfterCreation = 0.5f, float timeout = 30) where T : UiPresenter
		{
			Log("Waiting for screen " + typeof(T).Name + " to open!");
			yield return TestTools.UntilObjectOfType<T>(timeout);
			// Wait a little bit more to make sure the screen had time to open
			Log("Detected " + typeof(T).Name + " screen! Continuing!");

			yield return new WaitForSeconds(waitAfterCreation);
		}
		
		public IEnumerator WaitForPresenter2<T>(float waitAfterCreation = 0.5f, float timeout = 30) where T : UIPresenter2
		{
			Log("Waiting for screen " + typeof(T).Name + " to open!");
			yield return TestTools.UntilObjectOfType<T>(timeout);
			// Wait a little bit more to make sure the screen had time to open
			Log("Detected " + typeof(T).Name + " screen! Continuing!");

			yield return new WaitForSeconds(waitAfterCreation);
		}

		public UiPresenter GetFirstOpenScreen(Type[] types)
		{
			foreach (var type in types)
			{
				var screen = Object.FindObjectOfType(type) as UiPresenter;
				if (screen != null && screen.gameObject.activeSelf)
				{
					return screen;
				}
			}

			return null;
		}

		/// <summary>
		/// Types must implement UiScreen
		/// </summary>
		/// <param name="types"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public IEnumerator WaitForAny(Type[] types, float timeout = 30)
		{
			yield return TestTools.Until(() => GetFirstOpenScreen(types) != null, timeout, "Cannot find screen presenters " + types.ToString());
			// Wait a little bit more to make sure the screen had time to open
			Log("Detected one of the " + types + " screen! Continuing!");
		}

		public T GetPresenter<T>() where T : UiPresenter
		{
			return Object.FindObjectOfType<T>();
		}


		public IEnumerator WaitForGenericDialog(float timeout = 30f, string title = "")
		{
			yield return TestTools.Until(() =>
				{
					var dialog = Object.FindObjectOfType<GenericDialogPresenter>();
					return dialog != null && dialog.gameObject.activeSelf && (title == "" || dialog.Title == title);
				},
				timeout,
				$"Generic dialog with title {title} did not open.");
		}

		public IEnumerator WaitForGenericInputDialogAndInput(string inputText, string title = "", float timeout = 30f)
		{
			yield return TestTools.Until(() =>
				{
					var dialog = Object.FindObjectOfType<GenericInputDialogPresenter>();
					return dialog != null && dialog.gameObject.activeSelf && (title == "" || dialog.Title == title);
				},
				timeout,
				$"Generic input dialog with title {title} did not open.");

			// Wait a little to be able to view the inputs
			var presenter = Object.FindObjectOfType<GenericInputDialogPresenter>();
			var dialogRoot = presenter.GetComponent<UIDocument>().rootVisualElement;
			var input = dialogRoot.Q<TextField>();
			input.Focus();

			yield return new WaitForSeconds(1);
			input.value = inputText;
			yield return new WaitForSeconds(1);
			yield return TouchOnElementByName("ConfirmButton");
			yield return new WaitForSeconds(1);
		}


		public IEnumerator ClickNextButton()
		{
			yield return TouchOnElementByName("NextButton");
			yield return new WaitForSeconds(1);
		}


		public (UIDocument, VisualElement)? SearchForElementGlobally(string name, Func<UQueryBuilder<VisualElement>, UQueryBuilder<VisualElement>> queryProcessor = null)
		{
			foreach (var type in Services.GameUiService.GetAllVisibleUi())
			{
				var foundObject = Object.FindObjectOfType(type);
				if (foundObject == null || foundObject is not UiPresenter presenter || presenter.GetComponent<UIDocument>() == null)
				{
					continue;
				}

				var document = presenter.GetComponent<UIDocument>();
				var root = document.rootVisualElement;

				var builder = root.Query(name);
				if (queryProcessor != null)
				{
					builder = queryProcessor(builder);
				}

				var el = builder.First();

				if (el == null)
				{
					continue;
				}

				return (document, el);
			}

			return null;
		}

		public IEnumerator TouchOnElementByName(string name)
		{
			Log("Trying to touch element with name " + name);

			var searchResult = SearchForElementGlobally(name);
			if (searchResult == null)
			{
				Fail("Not found button " + name + " to click!");
				yield break;
			}

			yield return TouchOnElement(searchResult.Value.Item1.rootVisualElement, searchResult.Value.Item2);
		}

		public IEnumerator TouchOnElement(VisualElement root, VisualElement element)
		{
			var elementPosition = RuntimePanelUtils.ScreenToPanel(root.panel, element.GetPositionOnScreen(root, false));
			Log($"Touching on element {element.name}! Position on screen: " + elementPosition);
			yield return TouchOnScreen(elementPosition, root);
		}

		private IEnumerator TouchOnScreen(Vector2 position, VisualElement root)
		{
			using (EventBase down = MakePointerEvent(TouchPhase.Began, position))
				root.SendEvent(down);
			yield return new WaitForSeconds(0.5f);
			using (EventBase up = MakePointerEvent(TouchPhase.Ended, position))
				root.SendEvent(up);
		}

		private EventBase MakePointerEvent(TouchPhase phase, Vector2 position, EventModifiers modifiers = EventModifiers.None, int fingerId = 0)
		{
			var touch = MakeDefaultTouch();
			touch.fingerId = fingerId;
			touch.position = position;
			touch.phase = phase;

			switch (touch.phase)
			{
				case TouchPhase.Began:
					return PointerDownEvent.GetPooled(touch, modifiers);
				case TouchPhase.Moved:
					return PointerMoveEvent.GetPooled(touch, modifiers);
				case TouchPhase.Ended:
					return PointerUpEvent.GetPooled(touch, modifiers);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static Touch MakeDefaultTouch()
		{
			var touch = new Touch();
			touch.fingerId = 0;
			touch.rawPosition = touch.position;
			touch.deltaPosition = Vector2.zero;
			touch.deltaTime = 0;
			touch.tapCount = 1;
			touch.pressure = 0.5f;
			touch.maximumPossiblePressure = 1;
			touch.type = TouchType.Direct;
			touch.altitudeAngle = 0;
			touch.azimuthAngle = 0;
			touch.radius = 1;
			touch.radiusVariance = 0;

			return touch;
		}

		public UIHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}
	}
}