using System;
using UnityEngine.UIElements;

namespace FirstLight.UiService
{
	/// <summary>
	/// Helper methods for <see cref="UiService"/>.
	/// </summary>
	public static class UIServiceUtils
	{
		/// <summary>
		/// Attaches a view controller to a visual element, within a presenter. The view
		/// object is created and initialized instantly.
		/// </summary>
		[Obsolete]
		public static TElement AttachView<TElement, TView, TPData>(this TElement element,
			UiToolkitPresenterData<TPData> presenter, out TView view)
			where TElement : VisualElement
			where TPData : struct
			where TView : UIView, new()
		{
			presenter.AddView(element, view = new TView());
			return element;
		}
		
		/// <summary>
		/// Attaches a view controller to a visual element, within a presenter.
		/// </summary>
		[Obsolete]
		public static TElement AttachExistingView<TElement, TPData>(this TElement element,
																   UiToolkitPresenterData<TPData> presenter, UIView view)
			where TElement : VisualElement
			where TPData : struct
		{
			presenter.AddView(element, view);
			return element;
		}
	}
}