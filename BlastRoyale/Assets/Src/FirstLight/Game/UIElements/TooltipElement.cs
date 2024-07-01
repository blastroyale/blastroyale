using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FirstLight.Game.UIElements
{
	public enum TipDirection
	{
		Top,
		TopRight,
		Left,
		TopLeft,
		Right,
		BottomRight,
		Bottom,
		BottomLeft
	}

	/// <summary>
	/// Displays the might graphic.
	/// </summary>
	public class TooltipElement : VisualElement
	{
		static CustomStyleProperty<Color> s_FillColor = new ("--fill-color");
		static CustomStyleProperty<Color> s_BorderColor = new ("--border-color");
		static CustomStyleProperty<float> s_BorderRadius = new ("--border-radius");
		static CustomStyleProperty<float> s_BorderSize = new ("--border-width");
		static CustomStyleProperty<float> s_TipLength = new ("--tip-length");
		static CustomStyleProperty<float> s_TipWidth = new ("--tip-width");


		protected TipDirection tipPosition { get; set; }
		private Color _color;
		private Color _borderColorStyle;
		private float _borderWidthStyle;
		private float _borderRadiusStyle;
		private float _tipWidthStyle;
		private float _tipLengthStyle;

		private float _borderRadius;
		private float _cornerLength => _borderRadius;

		private float _tipPositionPercentage = 0.5f;


		private VisualElement _contentContainer;
		private VisualElement _content;

		private Vector2 _start;
		private Vector2 _end;


		public TooltipElement() : this(TipDirection.Left)
		{
		}

		public TooltipElement(TipDirection direction)
		{
			tipPosition = direction;
			AddToClassList("tooltip-element");
			_contentContainer = new VisualElement {name = "ContentContainer"};
			_contentContainer.AddToClassList("tooltip-element__content-container");
			_content = new VisualElement {name = "Content"};
			_content.AddToClassList("tooltip-element__content");
			_contentContainer.Add(_content);
			hierarchy.Add(_contentContainer);
			generateVisualContent += GenerateVisualContent;
			RegisterCallback<CustomStyleResolvedEvent>(ResolvedCustomStyle);
		}
		

		public override VisualElement contentContainer => _content;

		private void ResolvedCustomStyle(CustomStyleResolvedEvent evt)
		{
			customStyle.TryGetValue(s_FillColor, out _color);
			customStyle.TryGetValue(s_BorderColor, out _borderColorStyle);
			customStyle.TryGetValue(s_BorderSize, out _borderWidthStyle);
			customStyle.TryGetValue(s_TipLength, out _tipLengthStyle);
			customStyle.TryGetValue(s_TipWidth, out _tipWidthStyle);
			customStyle.TryGetValue(s_BorderRadius, out _borderRadius);
			CalculateOffsets(true);
		}

		void GenerateVisualContent(MeshGenerationContext context)
		{
			var painter = context.painter2D;
			painter.BeginPath();
			painter.lineWidth = _borderWidthStyle;
			painter.fillColor = _color;
			painter.strokeColor = _borderColorStyle;
			CalculateOffsets();


			if (_end.y - _start.y < _tipLengthStyle)
			{
				
				return;
			}


			// Top line, i don't understand why i need to devide by two here but fuck it
			painter.MoveTo(_start + new Vector2(_cornerLength / 2, 0));
			RenderTop(painter);
			RenderRight(painter);
			RenderBottom(painter);
			RenderLeft(painter);


			painter.Stroke();
			painter.Fill();
		}

		public void SetTip(TipDirection direction)
		{
			tipPosition = direction;
			MarkDirtyRepaint();
			CalculateOffsets(true);
		}

		private void CalculateOffsets(bool applyPadding = false)
		{
			var root = _tipLengthStyle / Mathf.Sqrt(2);
			Vector2 startOffset;
			Vector2 endOffset;
			switch (tipPosition)
			{
				case TipDirection.Top:
					startOffset = new Vector2(0, _tipLengthStyle);
					endOffset = Vector2.zero;
					break;
				case TipDirection.Right:
					startOffset = new Vector2(0, 0);
					endOffset = new Vector2(_tipLengthStyle, 0);
					break;

				case TipDirection.Bottom:
					startOffset = new Vector2(0, 0);
					endOffset = new Vector2(0, _tipLengthStyle);
					break;
				case TipDirection.Left:
					startOffset = new Vector2(_tipLengthStyle, 0);
					endOffset = Vector2.zero;
					break;

				case TipDirection.TopRight:
					startOffset = new Vector2(0, root);
					endOffset = new Vector2(root, 0);
					break;
				case TipDirection.TopLeft:
					startOffset = new Vector2(root, root);
					endOffset = Vector2.zero;
					break;
				case TipDirection.BottomLeft:
					startOffset = new Vector2(root, 0);
					endOffset = new Vector2(0, root);
					break;
				case TipDirection.BottomRight:
					startOffset = new Vector2(0, 0);
					endOffset = new Vector2(root, root);
					break;
				default:
					startOffset = new Vector2(0, 0);
					endOffset = Vector2.zero;
					break;
			}

			_start = startOffset;
			_end = new Vector2(contentRect.width, contentRect.height) - endOffset;

			if (applyPadding)
			{
				_contentContainer.style.paddingLeft = startOffset.x;
				_contentContainer.style.paddingTop = startOffset.y;
				_contentContainer.style.paddingRight = endOffset.x;
				_contentContainer.style.paddingBottom = endOffset.y;
			}
		}


		private void RenderTop(Painter2D painter)
		{
			if (tipPosition == TipDirection.TopRight)
			{
				var side = _tipWidthStyle / Mathf.Sqrt(2);
				var a = new Vector2(_end.x - side, _start.y);
				var b = new Vector2(_end.x, _start.y + side);
				var m = new Vector2((a.x + b.x) / 2, (a.y + b.y) / 2);
				var c = m + (new Vector2(_tipLengthStyle, -_tipLengthStyle));
				painter.LineTo(a);
				painter.LineTo(c);
				painter.LineTo(b);
				return;
			}

			if (tipPosition == TipDirection.Top)
			{
				var startsDick = _end.x * _tipPositionPercentage - _tipWidthStyle / 2;
				var endsDick = _end.x * _tipPositionPercentage + _tipWidthStyle / 2;

				painter.LineTo(new Vector2(startsDick, _start.y));
				painter.LineTo(new Vector2((startsDick) + _tipWidthStyle / 2, 0));
				painter.LineTo(new Vector2(endsDick, _start.y));
			}


			// Right top corner
			painter.ArcTo(new Vector2(_end.x, _start.y), new Vector2(_end.x, _start.y + _cornerLength), _borderRadius);
		}

		private void RenderRight(Painter2D painter)
		{
			//painter.ArcTo(new Vector2(width, height), new Vector2(width - _corner, height), _radius);

			if (tipPosition == TipDirection.BottomRight)
			{
				var side = _tipWidthStyle / Mathf.Sqrt(2);
				var a = new Vector2(_end.x, _end.y - side);
				var b = new Vector2(_end.x - side, _end.y);
				var m = new Vector2((a.x + b.x) / 2, (a.y + b.y) / 2);
				var c = m + (new Vector2(_tipLengthStyle, _tipLengthStyle));
				painter.LineTo(a);
				painter.LineTo(c);
				painter.LineTo(b);
				return;
			}

			if (tipPosition == TipDirection.Right)
			{
				var startsDick = _end.y * _tipPositionPercentage - _tipWidthStyle / 2;
				var endsDick = _end.y * _tipPositionPercentage + _tipWidthStyle / 2;

				painter.LineTo(new Vector2(_end.x, startsDick));
				painter.LineTo(new Vector2(_end.x + _tipLengthStyle, (startsDick) + _tipWidthStyle / 2));
				painter.LineTo(new Vector2(_end.x, endsDick));
			}


			painter.ArcTo(new Vector2(_end.x, _end.y), new Vector2(_end.x - _cornerLength, _end.y), _borderRadius);
		}


		private void RenderBottom(Painter2D painter)
		{
			if (tipPosition == TipDirection.BottomLeft)
			{
				var side = _tipWidthStyle / Mathf.Sqrt(2);
				var a = new Vector2(_start.x + side, _end.y);
				var b = new Vector2(_start.x, _end.y - side);
				var m = new Vector2((a.x + b.x) / 2, (a.y + b.y) / 2);
				var c = m + new Vector2(-_tipLengthStyle, _tipLengthStyle);
				painter.LineTo(a);
				painter.LineTo(c);
				painter.LineTo(b);
				return;
			}

			if (tipPosition == TipDirection.Bottom)
			{
				var startsDick = _end.x * _tipPositionPercentage + _tipWidthStyle / 2;
				var endsDick = _end.x * _tipPositionPercentage - _tipWidthStyle / 2;

				painter.LineTo(new Vector2(startsDick, _end.y));
				painter.LineTo(new Vector2((startsDick) - _tipWidthStyle / 2, _end.y + _tipLengthStyle));
				painter.LineTo(new Vector2(endsDick, _end.y));
			}


			painter.ArcTo(new Vector2(_start.x, _end.y), new Vector2(_start.x, _end.y - _cornerLength), _borderRadius);
		}

		private void RenderLeft(Painter2D painter)
		{
			//painter.ArcTo(new Vector2(width, height), new Vector2(width - _corner, height), _radius);

			if (tipPosition == TipDirection.TopLeft)
			{
				var side = _tipWidthStyle / Mathf.Sqrt(2);
				var a = new Vector2(_start.x, _start.y + side);
				var b = new Vector2(_start.x + side, _start.y);
				var m = new Vector2((a.x + b.x) / 2, (a.y + b.y) / 2);
				var c = m + (new Vector2(-_tipLengthStyle, -_tipLengthStyle));
				painter.LineTo(a);
				painter.LineTo(c);
				painter.LineTo(b);
				return;
			}

			if (tipPosition == TipDirection.Left)
			{
				var startsDick = _end.y * _tipPositionPercentage + _tipWidthStyle / 2;
				var endsDick = _end.y * _tipPositionPercentage - _tipWidthStyle / 2;

				painter.LineTo(new Vector2(_start.x, startsDick));
				painter.LineTo(new Vector2(0, (startsDick) - _tipWidthStyle / 2));
				painter.LineTo(new Vector2(_start.x, endsDick));
			}


			painter.ArcTo(new Vector2(_start.x, _start.y), new Vector2(_start.x + _cornerLength, _start.y), _borderRadius);
		}


		public new class UxmlFactory : UxmlFactory<TooltipElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlEnumAttributeDescription<TipDirection> _tipPosition = new ()
			{
				name = "tip-position",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = TipDirection.Top,
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((TooltipElement) ve).tipPosition = _tipPosition.GetValueFromBag(bag, cc);
			}
		}
	}
}