using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
		private IUiService _uiService;

		/// <summary>
		/// Requests the open status of the <see cref="UiPresenter"/>
		/// </summary>
		public bool IsOpen => gameObject.activeSelf;

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
		protected virtual async Task OnClosed()
		{
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

		internal virtual async Task InternalClose(bool destroy)
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
		internal override async Task InternalClose(bool destroy)
		{
			if (destroy)
			{
				base.InternalClose(true);
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
		internal override async Task InternalClose(bool destroy)
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

	public abstract class UiToolkitPresenterData<T> : UiCloseActivePresenterData<T> where T : struct
	{
		[SerializeField, Required] private UIDocument _document;
		[SerializeField] private GameObject _background;
		[SerializeField] private int _millisecondsToClose = 0;

		protected VisualElement Root;

		private readonly Dictionary<VisualElement, IUIView> _views = new();

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
		/// Adds a <see cref="IUIView"/> view to the list of views, and handles it's lifecycle events.
		/// </summary>
		public void AddView(VisualElement element, IUIView view)
		{
			_views.Add(element, view);
			view.Attached(element);
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

				// TODO: There has to be a better way to make this query
				Root.Query()
					.Where(ve => typeof(IUIView).IsAssignableFrom(ve.GetType()))
					.Build()
					.ForEach(e => { AddView(e, (IUIView) e); });
			}

			Root.EnableInClassList(UIConstants.CLASS_HIDDEN, true);
			StartCoroutine(MakeVisible());
			
			SubscribeToEvents();
		}

		private IEnumerator MakeVisible()
		{
			yield return new WaitForEndOfFrame();
			Root.EnableInClassList(UIConstants.CLASS_HIDDEN, false);
		}

		protected override async Task OnClosed()
		{
			Root.EnableInClassList(UIConstants.CLASS_HIDDEN, true);
			UnsubscribeFromEvents();
			await Task.Delay(_millisecondsToClose);
			if (_background != null)
			{
				_background.SetActive(false);
			}
		}
	}
}