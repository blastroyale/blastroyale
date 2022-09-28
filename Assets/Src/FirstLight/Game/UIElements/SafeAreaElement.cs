using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A visual element that sets it's margins to account for safe area on mobile devices.
	/// </summary>
	public class SafeAreaElement : VisualElement
	{
		public new class UxmlFactory : UxmlFactory<SafeAreaElement, UxmlTraits>
		{
		}

		public SafeAreaElement()
		{
			style.flexGrow = 1;
			style.flexShrink = 1;

			RegisterCallback<GeometryChangedEvent>(LayoutChanged);
		}

		private void LayoutChanged(GeometryChangedEvent e)
		{
			var safeArea = Screen.safeArea;

			try
			{
				var leftTop =
					RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safeArea.xMin, Screen.height - safeArea.yMax));
				var rightBottom =
					RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width - safeArea.xMax, safeArea.yMin));

				style.marginLeft = leftTop.x;
				style.marginTop = leftTop.y;
				style.marginRight = rightBottom.x;
				style.marginBottom = rightBottom.y;
			}
			catch (InvalidCastException)
			{
				// Can be ignored, only for editor.
			}
		}
	}
}