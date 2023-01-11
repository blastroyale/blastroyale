using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Contains logic for displaying tooltips.
	/// </summary>
	public static class TooltipUtils
	{
		/// <summary>
		/// Opens a tooltip with a string content.
		/// </summary>
		public static void OpenTooltip(this VisualElement element, VisualElement root, string content,
									   TooltipDirection direction = TooltipDirection.TopRight,
									   TooltipPosition position = TooltipPosition.BottomLeft,
									   int offsetX = 0, int offsetY = 0)

		{
			var tooltip = OpenTooltip(element, root, direction, position, offsetX, offsetY);

			var label = new Label(content);
			label.AddToClassList("tooltip__label");

			tooltip.Add(label);
		}

		/// <summary>
		/// Opens a tooltip with a string content.
		/// </summary>
		public static void OpenTooltip(this VisualElement element, VisualElement root, string[] tags,
									   TooltipDirection direction = TooltipDirection.TopRight,
									   TooltipPosition position = TooltipPosition.BottomLeft,
									   int offsetX = 0, int offsetY = 0)

		{
			var tooltip = OpenTooltip(element, root, direction, position, offsetX, offsetY);
			tooltip.AddToClassList("tooltip--tags");

			foreach (var tag in tags)
			{
				var label = new Label(tag);
				label.AddToClassList("tooltip__tag");

				tooltip.Add(label);
			}
		}

		/// <summary>
		/// Opens a tooltip with any custom VisualElement content.
		/// </summary>
		private static VisualElement OpenTooltip(this VisualElement element, VisualElement root,
												 TooltipDirection direction = TooltipDirection.TopRight,
												 TooltipPosition position = TooltipPosition.BottomLeft,
												 int offsetX = 0, int offsetY = 0)

		{
			var blocker = new VisualElement();
			root.Add(blocker);
			blocker.AddToClassList("tooltip-holder");
			blocker.RegisterCallback<ClickEvent, VisualElement>((_, ve) => { ve.RemoveFromHierarchy(); }, blocker,
				TrickleDown.TrickleDown);

			var tooltip = new VisualElement();

			tooltip.AddToClassList("tooltip");
			tooltip.AddToClassList($"tooltip--{direction.ToString().ToLowerInvariant()}");
			tooltip.RegisterCallback<GeometryChangedEvent>(ev =>
			{
				var rootBound = root.worldBound;
				var elBound = element.worldBound;
				var ttBound = ev.newRect;

				var pos = position switch
				{
					TooltipPosition.Center => elBound.position + new Vector2(elBound.width / 2f, -elBound.height / 2f),
					TooltipPosition.TopLeft => elBound.position + new Vector2(0f, -elBound.height),
					TooltipPosition.TopRight => elBound.position + new Vector2(elBound.width, -elBound.height),
					TooltipPosition.BottomLeft => elBound.position,
					TooltipPosition.BottomRight => elBound.position + new Vector2(elBound.width, 0f),
					_ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
				};

				pos.x -= rootBound.width - offsetX;
				pos.y += element.worldBound.height - offsetY;

				switch (direction)
				{
					case TooltipDirection.TopLeft:
						pos.x += ttBound.width;
						break;
					case TooltipDirection.TopRight:
						// Do nothing
						break;
					case TooltipDirection.BottomLeft:
						pos.x += ttBound.width;
						pos.y -= ttBound.height;
						break;
					case TooltipDirection.BottomRight:
						pos.y -= ttBound.height;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
				}

				tooltip.transform.position = pos;
			});

			tooltip.experimental.animation
				.Start(0f, 1f, 200, (ve, val) => ve.style.opacity = val)
				.Ease(Easing.Linear);

			blocker.Add(tooltip);

			return tooltip;
		}
	}

	public enum TooltipPosition
	{
		Center,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
	}

	public enum TooltipDirection
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
	}
}