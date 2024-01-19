using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.UIElements;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// The root base of the UI Presenter of the <seealso cref="IUiService"/>
	/// Implement this abstract class in order to execute the proper UI life cycle
	/// </summary>
	public abstract class UiPresenter : MonoBehaviour
	{
		protected IUiService _uiService;

		/// <summary>
		/// Requests the open status of the <see cref="UiPresenter"/>
		/// </summary>
		public bool IsOpen => gameObject.activeSelf;

		public async UniTask EnsureOpen()
		{
			while (!IsOpen) await UniTask.Yield();
			await UniTask.Yield(); // one frame to allow it to render
		}
		
		/// <summary>
		/// Sets the current presenter hidden or not.
		/// It will still be enabled and running just not showing.
		/// </summary>
		public bool Hidden { 
			get => GetComponent<Canvas>().enabled;
			set => GetComponent<Canvas>().enabled = !value;
		}

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when it is initialized
		/// </summary>
		protected virtual void OnInitialized()
		{
		}

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when it is opened
		/// </summary>
		protected virtual void OnOpened()
		{
		}

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when it is closed
		/// </summary>
		protected virtual UniTask OnClosed()
		{
			return UniTask.CompletedTask;
		}

		/// <summary>
		/// Allows the ui presenter implementation to directly close the ui presenter without needing to call the service directly
		/// </summary>
		protected virtual void Close(bool destroy)
		{
			_uiService.CloseUi(this, destroy);
		}

		internal void Init(IUiService uiService)
		{
			_uiService = uiService;
			OnInitialized();
		}

		internal void InternalOpen()
		{
			gameObject.SetActive(true);

			OnOpened();
		}

		internal virtual async UniTask InternalClose(bool destroy)
		{
			await OnClosed();

			if (gameObject == null)
			{
				return;
			}

			if (destroy)
			{
				_uiService.UnloadUi(GetType());
			}
			else
			{
				gameObject.SetActive(false);
			}
		}
	}

	/// <summary>
	/// This type of UI Presenter closes a menu but does not disable the game object the Presenter is on.
	/// The intention is for developers to implement subclasses with behaviour that turns off the game object after completing
	/// some behaviour first, e.g. playing an animation or timeline.
	/// </summary>
	public abstract class UiCloseActivePresenter : UiPresenter
	{
		internal override async UniTask InternalClose(bool destroy)
		{
			if (destroy)
			{
				await base.InternalClose(true);
			}
			else
			{
				await OnClosed();
			}
		}
	}

	/// <summary>
	/// Tags the <see cref="UiPresenter"/> as a <see cref="UiPresenterData{T}"/> to allow defining a specific state when
	/// opening the UI via the <see cref="UiService"/>
	/// </summary>
	public interface IUiPresenterData
	{
	}

	/// <summary>
	/// A temporary interface to allow access to the Document of UI Toolkit presenters
	/// </summary>
	public interface IUIDocumentPresenter
	{
		public UIDocument Document { get; }
	}

	/// <inheritdoc cref="UiPresenter"/>
	/// <remarks>
	/// Extends the <see cref="UiPresenter"/> behaviour with defined data of type <typeparamref name="T"/>
	/// </remarks>
	public abstract class UiPresenterData<T> : UiPresenter, IUiPresenterData where T : struct
	{
		/// <summary>
		/// The Ui data defined when opened via the <see cref="UiService"/>
		/// </summary>
		public T Data { get; protected set; }

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when the data defined for the presenter is set
		/// </summary>
		protected virtual void OnSetData()
		{
		}

		internal void InternalSetData(T data)
		{
			Data = data;

			OnSetData();
		}
	}

	/// <summary>
	/// Tags the <see cref="UiCloseActivePresenter"/> as a <see cref="UiCloseActivePresenterData{T}"/> to allow defining a specific state when
	/// opening the UI via the <see cref="UiService"/>
	/// </summary>
	public abstract class UiCloseActivePresenterData<T> : UiPresenterData<T> where T : struct
	{
		internal override async UniTask InternalClose(bool destroy)
		{
			if (destroy)
			{
				await base.InternalClose(true);
			}
			else
			{
				await OnClosed();
			}
		}
	}

	/// <summary>
	/// This class is the UiToolkit implementation of UiCloseActivePresenterData
	/// </summary>
	[LoadSynchronously]
	public abstract class UiToolkitPresenterData<T> : UiCloseActivePresenterData<T>, IUIDocumentPresenter
		where T : struct
	{
		[SerializeField, Required] private UIDocument _document;
		[SerializeField] private GameObject _background;
		[SerializeField] private int _millisecondsToClose = 0;

		protected VisualElement Root;
		private readonly Dictionary<VisualElement, UIView> _views = new();

		public UIDocument Document => _document;

		/// <summary>
		/// Called when the presenter is ready to have the <paramref name="root"/> <see cref="VisualElement"/> queried for elements.
		/// </summary>
		protected virtual void QueryElements(VisualElement root)
		{
		}

		/// <summary>
		/// Subscribe to callbacks / events. Triggered after <see cref="QueryElements"/>, on every screen open.
		/// </summary>
		protected virtual void SubscribeToEvents()
		{
			foreach (var (_, view) in _views)
			{
				view.SubscribeToEvents();
			}
		}

		/// <summary>
		/// Unsubscribe from callbacks / events.
		/// </summary>
		protected virtual void UnsubscribeFromEvents()
		{
			foreach (var (_, view) in _views)
			{
				view.UnsubscribeFromEvents();
			}
		}

		/// <summary>
		/// Adds a <see cref="UIView"/> view to the list of views, and handles it's lifecycle events.
		/// </summary>
		public void AddView(VisualElement element, UIView view)
		{
			_views.Add(element, view);
			view.Attached(element);
		}

		protected virtual void OnTransitionsReady()
		{
		}

		protected override void OnOpened()
		{
			if (_background != null)
			{
				_background.SetActive(true);
			}

			if (Root == null)
			{
				Root = _document.rootVisualElement.Q(UIConstants.ID_ROOT);
				QueryElements(Root);
			}

			Root.EnableInClassList(UIConstants.CLASS_HIDDEN, true);
			StartCoroutine(MakeVisible());

			SubscribeToEvents();
		}

		protected override async UniTask OnClosed()
		{
			Root.EnableInClassList(UIConstants.CLASS_HIDDEN, true);

			UnsubscribeFromEvents();

			await UniTask.Delay(_millisecondsToClose);

			if (_background != null)
			{
				_background.SetActive(false);
			}
		}

		private IEnumerator MakeVisible()
		{
			yield return new WaitForEndOfFrame();

			Root.EnableInClassList(UIConstants.CLASS_HIDDEN, false);

			OnTransitionsReady();
		}
	}

	/// <summary>
	/// This is an implementation of UiToolkitPresenterData that allows instantiation of a UiToolkitPresenterData
	/// without a StateData parameter as constructor 
	/// </summary>
	public abstract class UiToolkitPresenter : UiToolkitPresenterData<UiToolkitPresenter.StateData>
	{
		public struct StateData
		{
		}
	}
}