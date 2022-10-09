using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helper methods for UI Toolkit elements / documents.
	/// </summary>
	public static class UIUtils
	{
		/// <summary>
		/// Throws an exception if the <paramref name="visualElement"/> is null.
		/// </summary>
		public static T Required<T>(this T visualElement) where T : VisualElement
		{
			if (visualElement == null)
			{
				throw new NullReferenceException("VisualElement should not be null!");
			}

			return visualElement;
		}

		/// <summary>
		/// Gets the position (center of content rect) of the <paramref name="element"/>, in screen coordinates.
		/// </summary>
		public static Vector2 GetPositionOnScreen(this VisualElement element)
		{
			return element.LocalToWorld(element.contentRect.center);
		}
	}
}