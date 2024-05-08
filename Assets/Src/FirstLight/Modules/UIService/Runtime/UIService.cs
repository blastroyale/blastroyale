using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FirstLight.UIService
{
	/// <summary>
	/// Handles all of our common UI loading / layering logic (opening / closing screens).
	/// </summary>
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

		public event Action<Type> OnScreenOpened;

		private readonly GameObject _root;

		private readonly Dictionary<Type, UIPresenter> _openedScreensType = new ();
		private readonly Dictionary<UILayer, UIPresenter> _openedScreensLayer = new ();

		public UIService()
		{
			_root = new GameObject("UI");
			Object.DontDestroyOnLoad(_root);
		}

		/// <summary>
		/// Opens a new screen of type <typeparamref name="T"/>. If a screen is already opened on the same layer it will be closed.
		/// </summary>
		/// <param name="data">Optional data when using UIPresenterData</param>
		/// <typeparam name="T">The type of screen to open.</typeparam>
		/// <returns>A new UniTask that returns the presenter when it's finished.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the same screen type is already opened.</exception>
		public async UniTask<T> OpenScreen<T>(object data = null) where T : UIPresenter
		{
			var screenType = typeof(T);

			FLog.Info($"Opening screen {screenType.Name}");
			if (_openedScreensType.TryGetValue(screenType, out var openedScreenType))
			{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
				FLog.Error($"Screen {screenType.Name} is already opened!");
				return (T) openedScreenType;
#endif
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
			OnScreenOpened?.Invoke(typeof(T));
			return (T) screen;
		}

		/// <summary>
		/// LEGACY ONLY: Closes the provided screen.
		/// </summary>
		/// <param name="presenter">The presenter to close.</param>
		public async UniTask CloseScreen(UIPresenter presenter)
		{
			var screenType = presenter.GetType();

			Assert.IsTrue(_openedScreensType.ContainsKey(screenType), "Trying to close presenter that isn't open, how did you manage that?");

			await presenter.OnScreenClosedInternal();

			_openedScreensType.Remove(screenType);
			_openedScreensLayer.Remove(presenter.Layer);

			Addressables.ReleaseInstance(presenter.gameObject);
		}

		/// <summary>
		/// Close a screen of type <typeparamref name="T"/> if it's open.
		/// </summary>
		/// <param name="checkOpened">FOR LEGACY ONLY: If true will throw an exception when the screen is closed already.</param>
		/// <typeparam name="T">The type of screen to close</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the screen is not opened.</exception>
		public UniTask CloseScreen<T>(bool checkOpened = true) where T : UIPresenter
		{
			FLog.Info($"Closing screen {typeof(T).Name}");

			if (_openedScreensType.TryGetValue(typeof(T), out var screen))
			{
				return CloseScreen(screen);
			}

#if DEVELOPMENT_BUILD || UNITY_EDITOR
			if (checkOpened) FLog.Error($"Screen {typeof(T).Name} is not opened!");
#endif

			return UniTask.CompletedTask;
		}

		/// <summary>
		/// Closes a screen on the given layer.
		/// </summary>
		/// <param name="layer">The layer to close.</param>
		/// <param name="checkOpened">If we should log an error when this screen is not opened.</param>
		public UniTask CloseScreen(UILayer layer, bool checkOpened = true)
		{
			FLog.Info($"Closing layer {layer}");

			if (_openedScreensLayer.TryGetValue(layer, out var screen))
			{
				return CloseScreen(screen);
			}

#if DEVELOPMENT_BUILD || UNITY_EDITOR
			if (checkOpened) FLog.Error($"Layer {layer} is empty!");
#endif

			return UniTask.CompletedTask;
		}

		/// <summary>
		/// Returns the screen of type <typeparamref name="T"/> if it's open.
		///
		/// NOTE: This should not really be used, it's here for legacy reasons, think hard if you really need this.
		/// </summary>
		/// <typeparam name="T">The type of screen to get.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the screen you're trying to get is not opened</exception>
		public T GetScreen<T>() where T : UIPresenter
		{
			var screenType = typeof(T);
			if (_openedScreensType.TryGetValue(screenType, out var screen))
			{
				return (T) screen;
			}

			throw new InvalidOperationException($"Screen {screenType.Name} is not opened!");
		}


		/// <summary>
		/// Checks if a screen of type <typeparamref name="T"/> is open.
		/// </summary>
		public bool IsScreenOpen<T>() where T : UIPresenter
		{
			return _openedScreensType.ContainsKey(typeof(T));
		}

		private static string GetAddress<T>() where T : UIPresenter
		{
			return $"UI/{typeof(T).Name.Replace("Presenter", "")}.prefab";
		}
	}
}