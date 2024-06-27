using System;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the might graphic.
	/// </summary>
	public class AngledContainerElement : VisualElement
	{
#pragma warning disable CS0414 // Field is assigned but its value is never used
		protected int leftAngle { get; set; }
		protected int rightAngle { get; set; }
		protected int borderSize { get; set; }
		protected bool inverted { get; set; }
		protected bool useParentHeight { get; set; }
		protected Color fillColor { get; set; }
		protected Color outlineColor { get; set; }
		protected int outlineWidth { get; set; }

		private float _borderRadiusStyle = 5;
		private float _borderRadius = 5;
		private VectorImage cachedImage;
#pragma warning restore CS0414 // Field is assigned but its value is never used
		private VisualElement _contentContainer;
		private readonly VisualElement _background;
		private readonly VisualElement _mask;

		public override VisualElement contentContainer => _mask;

		public AngledContainerElement()
		{
			hierarchy.Add(_background = new VisualElement()
			{
				name = "Background",
				style =
				{
					position = Position.Absolute,
					left = 0,
					top = 0,
					height = Length.Percent(100),
					width = Length.Percent(100),
				}
			});
			hierarchy.Add(_mask = new VisualElement()
			{
				name = "Mask"
			});
			_background.generateVisualContent += GenerateBackground;
			//_mask.generateVisualContent += GenerateOutline;
			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			_mask.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedMask);
		}

		void GenerateBackground(MeshGenerationContext context)
		{
			var paint = this.contentRect;
			ToPainter(context.painter2D, paint, false, this.fillColor, outlineColor, borderSize, outlineWidth, useParentHeight ? parent.contentRect : default);
		}

		private void OnGeometryChangedMask(GeometryChangedEvent evt)
		{
			var newPainter = new Painter2D();
			ToPainter(newPainter, evt.newRect, true, Color.red, Color.red, borderSize, outlineWidth, this.contentRect);
			if (cachedImage == null)
			{
				cachedImage = ScriptableObject.CreateInstance<VectorImage>();
			}

			_mask.style.backgroundImage = new StyleBackground(cachedImage);
			newPainter.SaveToVectorImage(cachedImage);
			_mask.MarkDirtyRepaint();
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			this._mask.style.position = Position.Absolute;
			this._mask.style.top = 0;
			this._mask.style.left = 0;
			this._mask.style.height = contentRect.height;
			this._mask.style.width = contentRect.width;
			this._mask.style.overflow = Overflow.Hidden;
			this._mask.MarkDirtyRepaint();
		}

		public Rect ToPainter(Painter2D painter, Rect paintReact, bool mask, Color fillColor, Color strokeColor, float borderWidth, float strokeWidth = 0, Rect parentRect = default)
		{
			var useParent = parentRect != default;
			var size = paintReact.size;
			var offset = Vector2.zero;
			var isTopAnchor = resolvedStyle.top == 0;
			var topLeft = offset;
			var topRight = new Vector2(size.x, offset.y);
			var bottomRight = new Vector2(size.x, size.y);
			var bottomLeft = new Vector2(offset.x, size.y);

			var leftOffset = Mathf.Tan(Mathf.Deg2Rad * leftAngle) * size.y;
			var leftOffsetParent = Mathf.Tan(Mathf.Deg2Rad * leftAngle) * parentRect.size.y;
			var rightOffset = Mathf.Tan(Mathf.Deg2Rad * rightAngle) * size.y;
			var rightOffsetParent = Mathf.Tan(Mathf.Deg2Rad * rightAngle) * parentRect.size.y;
			var leftSizeReduction = Mathf.Tan(Mathf.Deg2Rad * leftAngle) * (parentRect.size.y - size.y);
			var rightSizeReduction = Mathf.Tan(Mathf.Deg2Rad * rightAngle) * (parentRect.size.y - size.y);
			var borderSizePixels = borderWidth;
			var xMp = inverted ? -1 : 1;
			var rightSin = Mathf.Sin((90 - rightAngle) * Mathf.Deg2Rad) * borderSizePixels;
			var rightCos = Mathf.Cos((90 - rightAngle) * Mathf.Deg2Rad) * borderSizePixels * xMp;
			var leftSin = Mathf.Sin((90 - leftAngle) * Mathf.Deg2Rad) * borderSizePixels;
			var leftCos = Mathf.Cos((90 - leftAngle) * Mathf.Deg2Rad) * borderSizePixels * xMp;
			if (inverted)
			{
				if (isTopAnchor && useParent)
				{
					bottomRight.x -= rightOffset;
					topLeft.x += leftOffsetParent;
					bottomLeft.x += leftSizeReduction;
				}
				else if (useParent)
				{
					bottomRight.x -= rightOffsetParent;
					topRight.x -= rightSizeReduction;
					topLeft.x += leftOffset;
				}
				else
				{
					bottomRight.x -= rightOffset;
					topLeft.x += leftOffset;
				}
			}
			else
			{
				if (isTopAnchor && useParent)
				{
					topRight.x -= rightOffsetParent;
					bottomLeft.x += leftOffset;
					bottomRight.x -= rightSizeReduction;
				}
				else if (useParent)
				{
					topRight.x -= rightOffset;
					topLeft.x += leftSizeReduction;
					bottomLeft.x += leftOffsetParent;
				}
				else
				{
					topRight.x -= rightOffset;
					bottomLeft.x += leftOffset;
				}
			}

			painter.BeginPath();
			painter.fillColor = fillColor;
			painter.lineJoin = LineJoin.Round;
			painter.lineCap = LineCap.Round;
			painter.MoveTo(topLeft + new Vector2(borderSizePixels, 0));
			// Top right corner
			{
				var startPoint = topRight + new Vector2(-borderSizePixels, 0);
				var endPoint = topRight + new Vector2(rightCos, rightSin);
				painter.LineTo(startPoint);
				painter.QuadraticCurveTo(topRight, endPoint);
			}
			// Bottom right corner
			{
				painter.LineTo(bottomRight + new Vector2(-rightCos, -rightSin));
				painter.QuadraticCurveTo(bottomRight, bottomRight + new Vector2(-borderSizePixels, 0));
			}
			//
			{
				var startPoint = bottomLeft + new Vector2(borderSizePixels, 0);
				var endPoint = bottomLeft + new Vector2(-leftCos, -leftSin);
				painter.LineTo(startPoint);
				painter.QuadraticCurveTo(bottomLeft, endPoint);
			}
			// top left
			{
				var startPoint = topLeft + new Vector2(leftCos, leftSin);
				var endPoint = topLeft + new Vector2(borderSizePixels, 0);
				painter.LineTo(startPoint);
				painter.QuadraticCurveTo(topLeft, endPoint);
			}
			if (strokeWidth > 0)
			{
				painter.strokeColor = strokeColor;
				painter.lineWidth = strokeWidth;
				painter.Stroke();
			}

			painter.Fill();
			painter.ClosePath();

			var min = Vector2.Min(
				bottomRight, Vector2.Min(
					topRight, Vector2.Min(
						topLeft,
						bottomLeft
					))
			);
			var max = Vector2.Max(
				bottomRight, Vector2.Max(
					topRight, Vector2.Max(
						topLeft,
						bottomLeft
					))
			);

			return new Rect(min, max - min);
		}

		public new class UxmlFactory : UxmlFactory<AngledContainerElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlIntAttributeDescription _leftAngle = new ()
			{
				name = "leftAngle",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = 0,
			};

			UxmlIntAttributeDescription _rightAngle = new ()
			{
				name = "rightAngle",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = 0,
			};

			UxmlIntAttributeDescription _borderSize = new ()
			{
				name = "borderSize",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = 20,
			};

			UxmlIntAttributeDescription _outlineWidth = new ()
			{
				name = "outlineWidth",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = 20,
			};

			UxmlBoolAttributeDescription _inverted = new ()
			{
				name = "inverted",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = false,
			};

			UxmlBoolAttributeDescription _useParentHeight = new ()
			{
				name = "useParentHeight",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = false,
			};

			private UxmlColorAttributeDescription _color = new ()
			{
				name = "fillColor",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = Color.blue,
			};

			private UxmlColorAttributeDescription _outlineColor = new ()
			{
				name = "outlineColor",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = Color.yellow,
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var an = ((AngledContainerElement) ve);
				an.leftAngle = _leftAngle.GetValueFromBag(bag, cc);
				an.rightAngle = _rightAngle.GetValueFromBag(bag, cc);
				an.inverted = _inverted.GetValueFromBag(bag, cc);
				an.useParentHeight = _useParentHeight.GetValueFromBag(bag, cc);
				an.fillColor = _color.GetValueFromBag(bag, cc);
				an.borderSize = _borderSize.GetValueFromBag(bag, cc);
				an.outlineWidth = _outlineWidth.GetValueFromBag(bag, cc);
				an.outlineColor = _outlineColor.GetValueFromBag(bag, cc);
				an.MarkDirtyRepaint();
			}
		}
	}

	public static class Vector2Extensions
	{
		public static Vector2 XO(this Vector2 vector2)
		{
			return new Vector2(vector2.x, 0);
		}

		public static Vector2 XX(this Vector2 vector2)
		{
			return new Vector2(vector2.x, vector2.x);
		}

		public static Vector2 OY(this Vector2 vector2)
		{
			return new Vector2(0, vector2.y);
		}

		public static Vector2 YY(this Vector2 vector2)
		{
			return new Vector2(vector2.y, vector2.y);
		}
	}
}