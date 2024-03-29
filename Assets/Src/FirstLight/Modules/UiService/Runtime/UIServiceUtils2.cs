using UnityEngine.UIElements;

namespace FirstLight.UIService
{
	public static class UIServiceUtils2
	{
		/// <summary>
		/// Attaches a view controller to a visual element, within a presenter. The view
		/// object is created and initialized instantly.
		/// </summary>
		public static TElement AttachView2<TElement, TView>(this TElement element,
														   UIPresenter2 presenter, out TView view)
			where TElement : VisualElement
			where TView : UIView2, new()
		{
			presenter.AddView(element, view = new TView());
			return element;
		}

		/// <summary>
		/// Attaches a view controller to a visual element, within a presenter.
		/// </summary>
		public static TElement AttachExistingView2<TElement>(this TElement element,
															UIPresenter2 presenter, UIView2 view)
			where TElement : VisualElement
		{
			presenter.AddView(element, view);
			return element;
		}
	}
}