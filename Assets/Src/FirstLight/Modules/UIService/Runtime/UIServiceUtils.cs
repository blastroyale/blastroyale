using System;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;

namespace FirstLight.UIService
{
	public static class UIServiceUtils
	{
		/// <summary>
		/// Attaches a view controller to a visual element, within a presenter. The view
		/// object is created and initialized instantly.
		/// </summary>
		public static TElement AttachView<TElement, TView>(this TElement element,
														   UIPresenter presenter, out TView view)
			where TElement : VisualElement
			where TView : UIView, new()
		{
			presenter.AddView(element, view = new TView());
			return element;
		}

		/// <summary>
		/// Attaches a view controller to a visual element, within a presenter.
		/// </summary>
		public static TElement AttachExistingView<TElement>(this TElement element,
															UIPresenter presenter, UIView view)
			where TElement : VisualElement
		{
			presenter.AddView(element, view);
			return element;
		}

		internal static T GetAttribute<T>(this ICustomAttributeProvider member) where T : Attribute
		{
			var attributes = member.GetCustomAttributes(typeof(T), false).Cast<T>().ToArray();
			return attributes.Length != 0 ? attributes[0] : default;
		}
	}
}