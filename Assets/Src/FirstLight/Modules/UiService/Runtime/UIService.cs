using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Modules.UIService.Runtime;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FirstLight.UIService
{
	public class UIService
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

		/// <summary>
		/// Class to use labels to display the player name, this is usually used bellow characters
		/// </summary>
		// TODO: Shouldn't be here
		public const string USS_PLAYER_LABEL = "player-name";

		private readonly GameObject _root;

		private readonly Dictionary<Type, UIPresenter> _openedScreensType = new ();
		private readonly Dictionary<UILayer, UIPresenter> _openedScreensLayer = new ();

		public UIService()
		{
			_root = new GameObject("UI");
			Object.DontDestroyOnLoad(_root);
		}

		public async UniTask<T> OpenScreen<T>(object data = null) where T : UIPresenter
		{
			var screenType = typeof(T);
			FLog.Info($"Opening screen {screenType.Name}");
			if (_openedScreensType.TryGetValue(screenType, out var existingScreen))
			{
				// FLog.Error($"Screen {typeof(T).Name} is already opened!");
				// return (T) existingScreen;

				throw new InvalidOperationException($"Screen {screenType.Name} is already opened");
			}

			var layer = screenType.GetAttribute<UILayerAttribute>()?.Layer ?? UILayer.Default;

			if (_openedScreensLayer.TryGetValue(layer, out var openedScreen))
			{
				await CloseScreen(openedScreen);
			}

			var handle = Addressables.InstantiateAsync(GetAddress<T>(), _root.transform);
			var go = handle.WaitForCompletion(); // Sync loading is intentional here
			var screen = go.GetComponent<UIPresenter>();
			var uiDocument = go.GetComponent<UIDocument>();

			screen.Layer = layer;
			screen.Data = data;
			uiDocument.sortingOrder = (int) layer;

			_openedScreensType.Add(typeof(T), screen);
			_openedScreensLayer.Add(layer, screen);

			await screen.OnScreenOpenedInternal();

			return (T) screen;
		}

		public bool IsScreenOpen<T>() where T : UIPresenter
		{
			return _openedScreensType.ContainsKey(typeof(T));
		}

		public T GetScreen<T>() where T : UIPresenter
		{
			var screenType = typeof(T);
			if (!_openedScreensType.TryGetValue(screenType, out var screen))
			{
				throw new InvalidOperationException($"Screen {screenType.Name} is not opened!");
			}

			return (T) screen;
		}

		public UniTask CloseScreen<T>() where T : UIPresenter
		{
			if (_openedScreensType.TryGetValue(typeof(T), out var screen))
			{
				FLog.Info($"Closing screen {typeof(T).Name}");
				return CloseScreen(screen);
			}

			// FLog.Error($"Screen {typeof(T).Name} is not opened!");
			throw new InvalidOperationException($"Screen {typeof(T).Name} is not opened!");
		}

		public async UniTask CloseScreen(UILayer layer)
		{
			FLog.Info($"Closing layer {layer}");
			await CloseScreen(_openedScreensLayer[layer]);


			if (_openedScreensLayer.TryGetValue(layer, out var screen))
			{
				FLog.Info($"Closing layer {layer}");
				await CloseScreen(screen);
			}
			else
			{
				throw new InvalidOperationException($"Layer {layer} is empty!");
			}
		}

		public async UniTask CloseScreen(UIPresenter presenter)
		{
			Assert.IsTrue(_openedScreensType.ContainsKey(presenter.GetType()), "Trying to close presenter that isn't open, how did you manage that?");

			await presenter.OnScreenClosedInternal();

			_openedScreensType.Remove(presenter.GetType());
			_openedScreensLayer.Remove(presenter.Layer);

			Addressables.ReleaseInstance(presenter.gameObject);
		}

		private static string GetAddress<T>() where T : UIPresenter
		{
			return $"UI/{typeof(T).Name.Replace("Presenter", "")}.prefab";
		}

		public enum UILayer
		{
			Background = -1,
			Default = 0,
			Popup = 1,
			Foreground = 2,

			LegacyVFXHack = 10,
		}
	}
}