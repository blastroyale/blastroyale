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

		/// <summary>
		/// Triggered when any screen is opened by this service.
		/// </summary>
		public event OnScreenOpenedDelegate OnScreenOpened;

		public delegate void OnScreenOpenedDelegate(string screenName, string layerName);

		private readonly GameObject _root;

		private readonly Dictionary<Type, UIPresenter> _openedScreensType = new ();
		private readonly Dictionary<UILayer, HashSet<UIPresenter>> _openedScreensLayer = new ();

		public UIService()
		{
			_root = new GameObject("UI");
			Object.DontDestroyOnLoad(_root);

			foreach (var layer in Enum.GetValues(typeof(UILayer)))
			{
				_openedScreensLayer.Add((UILayer) layer, new HashSet<UIPresenter>());
			}
		}

		/// <summary>
		/// Opens a new screen of type <typeparamref name="T"/>. If a screen is already opened on the same layer it will be closed.
		/// </summary>
		/// <param name="screenType">The Type of the presenter</param>
		/// <param name="data">Optional data when using UIPresenterData</param>
		/// <returns>A new UniTask that returns the presenter when it's finished.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the same screen type is already opened.</exception>
		public async UniTask<UIPresenter> OpenScreen(Type screenType, object data = null)
		{
			FLog.Info($"Opening screen {screenType.Name}");
			if (_openedScreensType.TryGetValue(screenType, out var openedScreenType))
			{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
				FLog.Error($"Screen {screenType.Name} is already opened!");
				return openedScreenType;
#endif
			}

			var uiLayer = screenType.GetAttribute<UILayerAttribute>();
			var layer = uiLayer?.Layer ?? UILayer.Default;

			await CloseLayer(layer, false);

			var handle = Addressables.InstantiateAsync(GetAddress(screenType), _root.transform);
			var go = handle.WaitForCompletion(); // Sync loading is intentional here
			var screen = go.GetComponent<UIPresenter>();
			var uiDocument = go.GetComponent<UIDocument>();

			screen.Layer = layer;
			screen.Data = data;
			uiDocument.sortingOrder = (int) layer;

			_openedScreensType.Add(screenType, screen);
			_openedScreensLayer[layer].Add(screen);
			await screen.OnScreenOpenedInternal(uiService: this);
			OnScreenOpened?.Invoke(screenType.Name.Replace("Presenter", ""), layer.ToString());
			return screen;
		}

		/// <summary>
		/// Close a screen of type <param name="screenType"/> if it's open.
		/// </summary>
		/// <param name="screenType">The type of screen to close</param>
		/// <param name="checkOpened">FOR LEGACY ONLY: If true will throw an exception when the screen is closed already.</param>
		/// <exception cref="InvalidOperationException">Thrown if the screen is not opened.</exception>
		public UniTask CloseScreen(Type screenType, bool checkOpened = true)
		{
			FLog.Info($"Closing screen {screenType.Name}");

			if (_openedScreensType.TryGetValue(screenType, out var screen))
			{
				return CloseScreen(screen);
			}

#if DEVELOPMENT_BUILD || UNITY_EDITOR
			if (checkOpened) FLog.Error($"Screen {screenType.Name} is not opened!");
#endif

			return UniTask.CompletedTask;
		}

		/// <summary>
		/// Closes all screens on a specific layer.
		/// </summary>
		/// <param name="layer">The layer to close</param>
		/// <param name="useAutoClose">If AutoClose=false presenters should be ignored.</param>
		public async UniTask CloseLayer(UILayer layer, bool useAutoClose = false)
		{
			var presenters = _openedScreensLayer[layer];
			var presentersToRemove = new List<UIPresenter>(presenters.Count);

			foreach (var presenter in presenters)
			{
				var screenType = presenter.GetType();
				var uiLayer = screenType.GetAttribute<UILayerAttribute>();

				if (useAutoClose && !uiLayer.AutoClose) continue;

				presentersToRemove.Add(presenter);
			}

			foreach (var presenter in presentersToRemove)
			{
				presenters.Remove(presenter);
				await CloseScreen(presenter);
			}
		}

		public bool HasUIPresenterOpenOnLayer(UILayer uiLayer)
		{
			return _openedScreensLayer.TryGetValue(uiLayer, out _);
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

		private async UniTask CloseScreen(UIPresenter presenter)
		{
			var screenType = presenter.GetType();

			await presenter.OnScreenClosedInternal();

			_openedScreensType.Remove(screenType);
			_openedScreensLayer[presenter.Layer].Remove(presenter);

			if (presenter)
				Addressables.ReleaseInstance(presenter.gameObject);
		}

		private static string GetAddress(Type type)
		{
			return $"UI/{type.Name.Replace("Presenter", "")}.prefab";
		}

		// Typed helpers
		/// <summary>
		/// Opens a new screen of type <typeparamref name="T"/>. If a screen is already opened on the same layer it will be closed.
		/// </summary>
		/// <param name="data">Optional data when using UIPresenterData</param>
		/// <typeparam name="T">The type of screen to open.</typeparam>
		/// <returns>A new UniTask that returns the presenter when it's finished.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the same screen type is already opened.</exception>
		public async UniTask<T> OpenScreen<T>(object data = null) where T : UIPresenter
		{
			return (T) await OpenScreen(typeof(T), data);
		}

		/// <summary>
		/// Close a screen of type <typeparamref name="T"/> if it's open.
		/// </summary>
		/// <param name="checkOpened">FOR LEGACY ONLY: If true will throw an exception when the screen is closed already.</param>
		/// <typeparam name="T">The type of screen to close</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the screen is not opened.</exception>
		public UniTask CloseScreen<T>(bool checkOpened = true) where T : UIPresenter
		{
			return CloseScreen(typeof(T), checkOpened);
		}
	}
}