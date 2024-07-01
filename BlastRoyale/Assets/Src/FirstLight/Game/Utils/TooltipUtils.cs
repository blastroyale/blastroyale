using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.UIElements;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Utils
{
	public enum TooltipPosition
	{
		Auto,
		Left,
		Right,
		Top,
		Bottom,
		TopRight,
		BottomRight,
		BottomLeft,
		TopLeft
	}

	public enum PlayerButtonContextStyle
	{
		Normal,
		Red
	}

	public class PlayerContextButton
	{
		public PlayerButtonContextStyle ContextStyle;
		public string Text;
		public Action OnClick;

		public PlayerContextButton()
		{
		}

		public PlayerContextButton(PlayerButtonContextStyle contextStyle, string text, Action onClick)
		{
			ContextStyle = contextStyle;
			Text = text;
			OnClick = onClick;
		}
	}

	/// <summary>
	/// Contains logic for displaying tooltips.
	/// </summary>
	public static class TooltipUtils
	{
		/// <summary>
		/// Opens a tooltip with a string content.
		/// </summary>
		public static void OpenTooltip(this VisualElement element, VisualElement root, string content,
									   Vector2 offset = default,
									   TooltipPosition position = TooltipPosition.Auto
		)

		{
			var tooltip = OpenTooltip(element.worldBound, root, offset, position);

			var label = new Label(content);
			label.AddToClassList("tooltip__label");

			tooltip.Add(label);
		}

		public static void OpenPlayerContextOptions(VisualElement element, VisualElement root, string playerName, IEnumerable<PlayerContextButton> buttons, TooltipPosition position = TooltipPosition.Auto)
		{
			var tooltip = OpenTooltip(element.worldBound, root, Vector2.zero, position);

			tooltip.AddToClassList("player-context-menu");
			var playerNameLabel = new Label(playerName);
			playerNameLabel.AddToClassList("player-context-menu__player-name");
			tooltip.Add(playerNameLabel);

			var buttonsHolder = new VisualElement();
			buttonsHolder.AddToClassList("player-context-menu__button-container");
			tooltip.Add(buttonsHolder);

			foreach (var playerContextButton in buttons)
			{
				var buttonElement = new Button(playerContextButton.OnClick);
				buttonElement.AddToClassList("player-context-menu__button");
				if (playerContextButton.ContextStyle == PlayerButtonContextStyle.Red)
				{
					buttonElement.AddToClassList("player-context-menu__button--red");
				}

				buttonElement.text = playerContextButton.Text;
				buttonsHolder.Add(buttonElement);
			}
		}

		/// <summary>
		/// Opens a tooltip with a string content.
		/// </summary>
		public static void OpenTooltip(this VisualElement element, VisualElement root, IEnumerable<string> tags,
									   Vector2 offset = default,
									   TooltipPosition position = TooltipPosition.Auto)

		{
			var tooltip = OpenTooltip(element.worldBound, root, offset, position);
			tooltip.AddToClassList("tooltip--tags");

			foreach (var tag in tags)
			{
				var label = new Label(tag);
				label.AddToClassList("tooltip__tag");

				tooltip.Add(label);
			}
		}

		/// <summary>
		/// Opens a tooltip with a string content.
		/// </summary>
		public static void OpenTooltip(this VisualElement element, VisualElement root, VisualElement content,
									   Vector2 offset = default,
									   TooltipPosition position = TooltipPosition.Auto)

		{
			var tooltip = OpenTooltip(element.worldBound, root, offset, position);
			tooltip.Add(content);
		}

		private static bool CanFit(Rect root, Vector2 tooltipPosition, Vector2 tooltipSize)
		{
			var tooltipRect = new Rect(new Vector2(tooltipPosition.x+root.width-tooltipSize.x, +tooltipPosition.y), tooltipSize);
			return root.Contains(tooltipRect.min) && root.Contains(tooltipRect.max);
		}

		/// <summary>
		/// Opens a tooltip with any custom VisualElement content.
		/// </summary>
		public static VisualElement OpenTooltip(Rect sourceRect, VisualElement root,
												Vector2 offet = default,
												TooltipPosition position = TooltipPosition.Auto)

		{
			var blocker = new VisualElement();
			root.Add(blocker);
			blocker.AddToClassList("tooltip-holder");
			blocker.RegisterCallback<ClickEvent, VisualElement>((_, ve) => { ve.RemoveFromHierarchy(); }, blocker,
				TrickleDown.TrickleDown);

			var tooltip = new TooltipElement(CalculateTipDirection(position));
			bool trigerred = false;
			tooltip.RegisterCallback<GeometryChangedEvent>(ev =>
			{
				var rootBound = root.worldBound;
				var tooltipRect = ev.newRect;

				var pos = Vector2.zero;
				if (position == TooltipPosition.Auto)
				{
					foreach (var value in Enum.GetValues(typeof(TooltipPosition)).Cast<TooltipPosition>())
					{
						if (value == TooltipPosition.Auto) continue;
						var tempPos = CalculatePosition(rootBound, sourceRect, tooltipRect, value, offet);
						if (!CanFit(rootBound, tempPos, tooltipRect.size)) continue;
						position = value;
						pos = tempPos;
						break;
					}
				}
				else
				{
					pos = CalculatePosition(rootBound, sourceRect, tooltipRect, position, offet);
				}

				tooltip.transform.position = pos;
				tooltip.SetTip(CalculateTipDirection(position));
				tooltip.MarkDirtyRepaint();
				if (trigerred) return;
				trigerred = true;
				if (position == TooltipPosition.Top)
				{
					var originalHeight = tooltip.contentRect.height;
					tooltip.experimental.animation.Start(0, 1, 200, (ve, val) =>
					{
						tooltip.style.height = originalHeight * val;
					}).Ease(Easing.OutBack).OnCompleted(() => { });
					return;
				}

				tooltip.experimental.animation
					.Start(0f, 1f, 200, (ve, val) => ve.style.opacity = val)
					.Ease(Easing.Linear);
			});

			blocker.Add(tooltip);

			return tooltip;
		}

		private static Vector2 CalculatePosition(Rect rootBound, Rect sourceRect, Rect tooltipRect, TooltipPosition position, Vector2 offset)
		{
			var pos = sourceRect.position;
			var sX = sourceRect.width;
			var sY = sourceRect.height;
			var tX = tooltipRect.width;
			var tY = tooltipRect.height;

			pos += position switch
			{
				TooltipPosition.Top         => new Vector2(sX / 2 + tX / 2, -tY - sY),
				TooltipPosition.TopRight    => new Vector2(sX + tX, -tY - sY),
				TooltipPosition.Right       => new Vector2(sX + tX, -sY / 2 - tY / 2),
				TooltipPosition.BottomRight => new Vector2(sX + tX, 0),
				TooltipPosition.Bottom      => new Vector2(sX / 2 + tX / 2, 0),
				TooltipPosition.BottomLeft  => new Vector2(0, 0),
				TooltipPosition.Left        => new Vector2(0, -sY / 2 - tY / 2),
				TooltipPosition.TopLeft     => new Vector2(0, -tY - sY),
				_                           => Vector2.zero
			};
			pos.x -= rootBound.width - offset.x;
			pos.y += sourceRect.height - offset.y;
			return pos;
		}

		private static TipDirection CalculateTipDirection(TooltipPosition position)
		{
			return position switch
			{
				TooltipPosition.Top         => TipDirection.Bottom,
				TooltipPosition.TopRight    => TipDirection.BottomLeft,
				TooltipPosition.Right       => TipDirection.Left,
				TooltipPosition.BottomRight => TipDirection.TopLeft,
				TooltipPosition.Bottom      => TipDirection.Top,
				TooltipPosition.BottomLeft  => TipDirection.TopRight,
				TooltipPosition.Left        => TipDirection.Right,
				TooltipPosition.TopLeft     => TipDirection.BottomRight,
				_                           => TipDirection.Bottom
			};
		}
	}
}