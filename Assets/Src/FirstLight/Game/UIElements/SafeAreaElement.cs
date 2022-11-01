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
		private bool _safeAreaTop = true;
		private bool _safeAreaBottom = true;
		private bool _safeAreaLeft = true;
		private bool _safeAreaRight = true;

		public SafeAreaElement()
		{
			style.flexGrow = 1;
			style.flexShrink = 1;

			pickingMode = PickingMode.Ignore; // To allow raycasts to pass through it

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

				if (_safeAreaTop) style.marginTop = leftTop.y;
				if (_safeAreaBottom) style.marginBottom = rightBottom.y;
				if (_safeAreaLeft) style.marginLeft = leftTop.x;
				if (_safeAreaRight) style.marginRight = rightBottom.x;
			}
			catch (InvalidCastException)
			{
				// Can be ignored, only for editor.
			}
		}

		public new class UxmlFactory : UxmlFactory<SafeAreaElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlBoolAttributeDescription _applyTopAttribute = new()
			{
				name = "apply-top",
				defaultValue = true,
				use = UxmlAttributeDescription.Use.Optional
			};

			private readonly UxmlBoolAttributeDescription _applyBottomAttribute = new()
			{
				name = "apply-bottom",
				defaultValue = true,
				use = UxmlAttributeDescription.Use.Optional
			};

			private readonly UxmlBoolAttributeDescription _applyLeftAttribute = new()
			{
				name = "apply-left",
				defaultValue = true,
				use = UxmlAttributeDescription.Use.Optional
			};

			private readonly UxmlBoolAttributeDescription _applyRightAttribute = new()
			{
				name = "apply-right",
				defaultValue = true,
				use = UxmlAttributeDescription.Use.Optional
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((SafeAreaElement) ve)._safeAreaTop = _applyTopAttribute.GetValueFromBag(bag, cc);
				((SafeAreaElement) ve)._safeAreaBottom = _applyBottomAttribute.GetValueFromBag(bag, cc);
				((SafeAreaElement) ve)._safeAreaLeft = _applyLeftAttribute.GetValueFromBag(bag, cc);
				((SafeAreaElement) ve)._safeAreaRight = _applyRightAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}