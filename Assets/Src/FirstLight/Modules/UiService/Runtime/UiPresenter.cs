using System;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// The root base of the UI Presenter of the <seealso cref="IUiService"/>
	/// Implement this abstract class in order to execute the proper UI life cycle
	/// </summary>
	[RequireComponent(typeof(Canvas))]
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
		protected virtual void OnInitialized() {}
		
		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when it is opened
		/// </summary>
		protected virtual void OnOpened() {}

		/// <summary>
		/// Allows the ui presenter implementation to have extra behaviour when it is closed
		/// </summary>
		protected virtual void OnClosed() {}

		/// <summary>
		/// Allows the ui presenter implementation to directly close the ui presenter without needing to call the service directly
		/// </summary>
		protected virtual void Close()
		{
			_uiService.CloseUi(this);
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

		internal virtual void InternalClose()
		{
			OnClosed();
			
			if (gameObject != null)
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
		internal override void InternalClose()
		{
			OnClosed();
		}
	}

	/// <summary>
	/// Tags the <see cref="UiPresenter"/> as a <see cref="UiPresenterData{T}"/> to allow defining a specific state when
	/// opening the UI via the <see cref="UiService"/>
	/// </summary>
	public interface IUiPresenterData {}

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
		protected virtual void OnSetData() {}

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
		internal override void InternalClose()
		{
			OnClosed();
		}
	}
}