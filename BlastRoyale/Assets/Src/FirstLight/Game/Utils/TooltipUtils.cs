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
									   TipDirection direction = TipDirection.TopRight,
									   TooltipPosition position = TooltipPosition.BottomLeft,
									   int offsetX = 0, int offsetY = 0)

		{
			var tooltip = OpenTooltip(element.worldBound, root, direction, position, offsetX, offsetY);

			var label = new Label(content);
			label.AddToClassList("tooltip__label");

			tooltip.Add(label);
		}

		public static void OpenPlayerContextOptions(VisualElement element, VisualElement root, string playerName, IEnumerable<PlayerContextButton> buttons, TipDirection direction = TipDirection.Bottom, TooltipPosition position = TooltipPosition.Top)
		{
			var tooltip = OpenTooltip(element.worldBound, root, direction, position);

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
									   TipDirection direction = TipDirection.TopRight,
									   TooltipPosition position = TooltipPosition.BottomLeft,
									   int offsetX = 0, int offsetY = 0)

		{
			var tooltip = OpenTooltip(element.worldBound, root, direction, position, offsetX, offsetY);
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
									   TipDirection direction = TipDirection.TopRight,
									   TooltipPosition position = TooltipPosition.BottomLeft,
									   int offsetX = 0, int offsetY = 0)

		{
			var tooltip = OpenTooltip(element.worldBound, root, direction, position, offsetX, offsetY);
			tooltip.Add(content);
		}

		/// <summary>
		/// Opens a tooltip with any custom VisualElement content.
		/// </summary>
		public static VisualElement OpenTooltip(Rect sourcePos, VisualElement root,
												TipDirection direction = TipDirection.TopRight,
												TooltipPosition position = TooltipPosition.BottomLeft,
												int offsetX = 0, int offsetY = 0)

		{
			var blocker = new VisualElement();
			root.Add(blocker);
			blocker.AddToClassList("tooltip-holder");
			blocker.RegisterCallback<ClickEvent, VisualElement>((_, ve) => { ve.RemoveFromHierarchy(); }, blocker,
				TrickleDown.TrickleDown);

			var tooltip = new TooltipElement(direction);
			bool trigerred = false;
			tooltip.RegisterCallback<GeometryChangedEvent>(ev =>
			{
				var rootBound = root.worldBound;
				var ttBound = ev.newRect;

				var pos = position switch
				{
					TooltipPosition.Center      => sourcePos.position + new Vector2(sourcePos.width / 2f, -sourcePos.height / 2f),
					TooltipPosition.CenterLeft  => sourcePos.position + new Vector2(0f, -sourcePos.height / 2f),
					TooltipPosition.CenterRight => sourcePos.position + new Vector2(sourcePos.width, -sourcePos.height / 2f),
					TooltipPosition.TopLeft     => sourcePos.position + new Vector2(0f, -sourcePos.height),
					TooltipPosition.TopRight    => sourcePos.position + new Vector2(sourcePos.width, -sourcePos.height),
					TooltipPosition.BottomLeft  => sourcePos.position,
					TooltipPosition.BottomRight => sourcePos.position + new Vector2(sourcePos.width, 0f),
					TooltipPosition.Top => sourcePos.position +
						new Vector2(sourcePos.width / 2f, -sourcePos.height) +
						new Vector2(ttBound.width / 2, 0),
					_ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
				};

				pos.x -= rootBound.width - offsetX;
				pos.y += sourcePos.height - offsetY;

				switch (direction)
				{
					case TipDirection.TopLeft:
						pos.x += ttBound.width;
						break;
					case TipDirection.TopRight:
						// Do nothing
						//pos.x -= ttBound.width;
						break;
					case TipDirection.BottomLeft:
						pos.x += ttBound.width;
						pos.y -= ttBound.height;
						break;
					case TipDirection.BottomRight:
						pos.y -= ttBound.height;
						break;
					case TipDirection.Right:
						pos.x += ttBound.width;
						break;
					case TipDirection.Left:
						pos.x += ttBound.width;
						break;
					case TipDirection.Top:
						pos.x += ttBound.width;
						break;
					case TipDirection.Bottom:
						pos.y -= ttBound.height;
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
				}

				tooltip.transform.position = pos;
				if (trigerred) return;
				trigerred = true;
				if (direction == TipDirection.Bottom)
				{
					var originalHeight = tooltip.contentRect.height;
					tooltip.experimental.animation.Start(0, 1, 200, (ve, val) =>
					{
						tooltip.style.height = originalHeight * val;
					}).Ease( Easing.OutBack).OnCompleted(() => { });
					return;
				}

				tooltip.experimental.animation
					.Start(0f, 1f, 200, (ve, val) => ve.style.opacity = val)
					.Ease(Easing.Linear);
			});


			blocker.Add(tooltip);

			return tooltip;
		}
	}

	public enum TooltipPosition
	{
		Center,
		Top,
		CenterLeft,
		CenterRight,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
	}
}