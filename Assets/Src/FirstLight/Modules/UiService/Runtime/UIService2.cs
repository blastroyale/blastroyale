using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FirstLight.UIService
{
	public class UIService2
	{
		/// <summary>
		/// The name that should be set to the root VisualElement in a screen.
		/// </summary>
		public const string ID_ROOT = "root";

		/// <summary>
		/// This class is toggled on the root VisualElement when opening / closing screens.
		/// </summary>
		public const string CLASS_HIDDEN = "hidden";

		/// <summary>
		/// Adds the forward click SFX to the element (on pointer down).
		/// </summary>
		// TODO: Shouldn't be here
		public const string SFX_CLICK_FORWARDS = "sfx-click-forwards";

		/// <summary>
		/// Adds the backwards click SFX to the element (on pointer down).
		/// </summary>
		// TODO: Shouldn't be here
		public const string SFX_CLICK_BACKWARDS = "sfx-click-backwards";

		private readonly GameObject _root;

		private readonly Dictionary<Type, UIPresenter2> _openedScreensType = new ();
		private readonly Dictionary<UILayer, UIPresenter2> _openedScreensLayer = new ();

		public UIService2()
		{
			_root = new GameObject("UI");
			Object.DontDestroyOnLoad(_root);
		}

		public async UniTask<T> OpenScreen<T>(object data = null, UILayer layer = UILayer.Default) where T : UIPresenter2
		{
			FLog.Info($"Opening screen {typeof(T).Name}");
			if (_openedScreensType.ContainsKey(typeof(T)))
			{
				throw new InvalidOperationException($"Screen {typeof(T).Name} is already opened");
			}

			if (_openedScreensLayer.TryGetValue(layer, out var openedScreen))
			{
				await CloseScreen(openedScreen);
			}

			var handle = Addressables.InstantiateAsync(GetAddress<T>(), _root.transform);
			var go = handle.WaitForCompletion(); // Sync loading is intentional here
			var screen = go.GetComponent<UIPresenter2>();
			var uiDocument = go.GetComponent<UIDocument>();

			screen.CurrentLayer = layer;
			screen.Data = data;
			uiDocument.sortingOrder = (int) layer;

			_openedScreensType.Add(typeof(T), screen);
			_openedScreensLayer.Add(layer, screen);

			await screen.OnScreenOpenedInternal();

			return (T) screen;
		}

		public async UniTask CloseScreen<T>() where T : UIPresenter2
		{
			FLog.Info($"Closing screen {typeof(T).Name}");
			await CloseScreen(_openedScreensType[typeof(T)]);
		}

		public async UniTask CloseScreen(UILayer layer)
		{
			FLog.Info($"Closing layer {layer}");
			await CloseScreen(_openedScreensLayer[layer]);
		}

		private async UniTask CloseScreen(UIPresenter2 presenter)
		{
			await presenter.OnScreenClosedInternal();

			_openedScreensType.Remove(presenter.GetType());
			_openedScreensLayer.Remove(presenter.CurrentLayer);

			Addressables.ReleaseInstance(presenter.gameObject);
		}

		private static string GetAddress<T>() where T : UIPresenter2
		{
			return $"UI/{typeof(T).Name.Replace("Presenter", "")}.prefab";
		}

		public enum UILayer
		{
			Background = -1,
			Default = 0,
			Popup = 1,
			Foreground = 2
		}
	}
}