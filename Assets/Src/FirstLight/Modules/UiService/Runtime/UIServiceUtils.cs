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
		public static void AttachView<TElement, TView, TPData>(this TElement element,
			UiToolkitPresenterData<TPData> presenter)
			where TElement : VisualElement
			where TPData : struct
			where TView : IUIView, new()
		{
			presenter.AddView(element, new TView());
		}
	}
}