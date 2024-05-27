using UnityEngine.UIElements;

namespace FirstLight.UIService
{
	public class UIView
	{
		/// <summary>
		/// The element this view is attached to.
		/// </summary>
		public VisualElement Element { get; private set; }
		
		/// <summary>
		/// Presenter this view is attached to.
		/// </summary>
		public UIPresenter Presenter { get; private set; }
		
		/// <summary>
		/// Called once, the first time the presenter screen is opened.
		/// </summary>
		protected virtual void Attached()
		{
		}

		/// <summary>
		/// Called when runtime logic should be initialized (subscribing to events etc...)
		/// </summary>
		/// <param name="reload"></param>
		public virtual void OnScreenOpen(bool reload)
		{
		}

		/// <summary>
		/// Called when runtime logic should be initialized (subscribing to events etc...)
		/// </summary>
		public virtual void OnScreenClose()
		{
		}

		/// <summary>
		/// Method called when attached to a presenter, if you need custom behaviour overwrite <see cref="Attached"/>
		/// </summary>
		internal void AttachedInternal(VisualElement element, UIPresenter presenter)
		{
			Element = element;
			Presenter = presenter;
			Attached();
		}
	}
}