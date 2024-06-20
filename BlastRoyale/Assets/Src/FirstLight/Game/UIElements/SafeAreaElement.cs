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
		private bool applyTop { get; set; }
		private bool applyBottom { get; set; }
		private bool applyLeft { get; set; }
		private bool applyRight { get; set; }
		private bool invert { get; set; }

		public SafeAreaElement() : this(true)
		{
		}

		public SafeAreaElement(bool applyTop = true, bool applyBottom = true, bool applyLeft = true,
							   bool applyRight = true)
		{
			this.applyTop = applyTop;
			this.applyBottom = applyBottom;
			this.applyLeft = applyLeft;
			this.applyRight = applyRight;

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

				if (applyTop) style.marginTop = invert ? -leftTop.y : leftTop.y;
				if (applyBottom) style.marginBottom = invert ? -rightBottom.y : rightBottom.y;
				if (applyLeft) style.marginLeft = invert ? -leftTop.x : leftTop.x;
				if (applyRight) style.marginRight = invert ? -rightBottom.x : rightBottom.x;
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

			private readonly UxmlBoolAttributeDescription _invertAttribute = new()
			{
				name = "invert",
				defaultValue = false,
				use = UxmlAttributeDescription.Use.Optional
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((SafeAreaElement) ve).applyTop = _applyTopAttribute.GetValueFromBag(bag, cc);
				((SafeAreaElement) ve).applyBottom = _applyBottomAttribute.GetValueFromBag(bag, cc);
				((SafeAreaElement) ve).applyLeft = _applyLeftAttribute.GetValueFromBag(bag, cc);
				((SafeAreaElement) ve).applyRight = _applyRightAttribute.GetValueFromBag(bag, cc);
				((SafeAreaElement) ve).invert = _invertAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}