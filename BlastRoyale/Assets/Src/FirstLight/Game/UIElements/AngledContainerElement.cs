using System;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the might graphic.
	/// TODO: Borders don't work so great
	/// </summary>
	public class AngledContainerElement : ImageButton
	{
		private static readonly CustomStyleProperty<Color> S_FillColor = new ("--fill-color");
		private static readonly CustomStyleProperty<Color> S_BorderColor = new ("--border-color");
		private static readonly CustomStyleProperty<int> S_BorderWidth = new ("--border-width");
		private static readonly CustomStyleProperty<int> S_BorderRadius = new ("--border-radius");
		private static readonly CustomStyleProperty<int> S_LeftAngle = new ("--left-angle");
		private static readonly CustomStyleProperty<int> S_RightAngle = new ("--right-angle");
		private static readonly CustomStyleProperty<bool> S_Inverted = new ("--inverted");
		private static readonly CustomStyleProperty<bool> S_UseParentHeight = new ("--use-parent-height");
#pragma warning disable CS0414 // Field is assigned but its value is never used
		private int _leftAngle;
		private int _rightAngle;
		private int _borderRadius;
		private int _borderWidth;
		private Color _borderColor;
		private bool _inverted;
		private bool _useParentHeight;
		private Color _fillColor;
#pragma warning restore CS0414 // Field is assigned but its value is never used
		private VectorImage cachedImage;
		private VisualElement _contentContainer;
		private readonly VisualElement _background;
		private readonly VisualElement _mask;
		private Vector2[] _vertexes = null;

		public override VisualElement contentContainer => _mask;

		public AngledContainerElement()
		{
			hierarchy.Add(_background = new VisualElement
			{
				name = "Background",
				style =
				{
					position = Position.Absolute,
					left = 0,
					top = 0,
					height = Length.Percent(100),
					width = Length.Percent(100),
				},
				pickingMode = PickingMode.Ignore
			});
			hierarchy.Add(_mask = new VisualElement
			{
				name = "Mask",
				pickingMode = PickingMode.Ignore
			});
			_background.generateVisualContent += GenerateBackground;
			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			_mask.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedMask);
			RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			if (_vertexes == null) return false;

			if (!base.ContainsPoint(localPoint))
			{
				return false;
			}

			var a = resolvedStyle.scale.value;
			var scale = new Vector3(a.x, a.y);
			var value = IsPointInQuadrilateral(new Vector2(localPoint.x, localPoint.y), _vertexes[0], _vertexes[1] * scale, _vertexes[2] * scale, _vertexes[3] );
			return value;
		}

		public override bool Overlaps(Rect rectangle)
		{
			var overlaps = ContainsPoint(rectangle.min) && ContainsPoint(rectangle.max) && ContainsPoint(new Vector2(rectangle.xMin, rectangle.yMax)) && ContainsPoint(new Vector2(rectangle.xMax, rectangle.yMin));

			Debug.Log("OVerlaps:" + overlaps);
			return overlaps;
		}

		static float CrossProduct(Vector2 A, Vector2 B, Vector2 P)
		{
			return (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);
		}

		static bool IsPointInQuadrilateral(Vector2 P, Vector2 A, Vector2 B, Vector2 C, Vector2 D)
		{
			// Adjust for inverted Y-axis by negating the Y-values
			Vector2 PA = new Vector2(P.x, -P.y);
			Vector2 AA = new Vector2(A.x, -A.y);
			Vector2 BA = new Vector2(B.x, -B.y);
			Vector2 CA = new Vector2(C.x, -C.y);
			Vector2 DA = new Vector2(D.x, -D.y);

			// Calculate cross products
			float cp1 = CrossProduct(AA, BA, PA);
			float cp2 = CrossProduct(BA, CA, PA);
			float cp3 = CrossProduct(CA, DA, PA);
			float cp4 = CrossProduct(DA, AA, PA);

			// Check if all cross products have the same sign
			return (cp1 > 0 && cp2 > 0 && cp3 > 0 && cp4 > 0) || (cp1 < 0 && cp2 < 0 && cp3 < 0 && cp4 < 0);
		}

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
		{
			if (evt.customStyle.TryGetValue(S_FillColor, out var color))
			{
				_fillColor = color;
			}

			if (evt.customStyle.TryGetValue(S_BorderColor, out var borderColor))
			{
				_borderColor = borderColor;
			}

			if (evt.customStyle.TryGetValue(S_BorderRadius, out var borderRadius))
			{
				_borderRadius = borderRadius;
			}

			if (evt.customStyle.TryGetValue(S_BorderWidth, out var borderWidth))
			{
				_borderWidth = borderWidth;
			}

			if (evt.customStyle.TryGetValue(S_LeftAngle, out var leftAngle))
			{
				_leftAngle = leftAngle;
			}

			if (evt.customStyle.TryGetValue(S_RightAngle, out var rightAngle))
			{
				_rightAngle = rightAngle;
			}

			if (evt.customStyle.TryGetValue(S_UseParentHeight, out var useParentHeight))
			{
				_useParentHeight = useParentHeight;
			}

			if (evt.customStyle.TryGetValue(S_Inverted, out var inverted))
			{
				_inverted = inverted;
			}
		}

		void GenerateBackground(MeshGenerationContext context)
		{
			var paint = contentRect;
			ToPainter(context.painter2D, paint, false, _fillColor, _borderColor, _borderRadius, _borderWidth, _useParentHeight ? parent.contentRect : default);
		}

		private void OnGeometryChangedMask(GeometryChangedEvent evt)
		{
			var newPainter = new Painter2D();
			ToPainter(newPainter, evt.newRect, true, Color.clear, Color.clear, _borderRadius, _borderWidth, contentRect);
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
			_mask.style.position = Position.Absolute;
			_mask.style.top = 0;
			_mask.style.left = 0;
			_mask.style.height = contentRect.height;
			_mask.style.width = contentRect.width;
			_mask.style.overflow = style.overflow;
			_mask.MarkDirtyRepaint();
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

			var leftOffset = Mathf.Tan(Mathf.Deg2Rad * _leftAngle) * size.y;
			var leftOffsetParent = Mathf.Tan(Mathf.Deg2Rad * _leftAngle) * parentRect.size.y;
			var rightOffset = Mathf.Tan(Mathf.Deg2Rad * _rightAngle) * size.y;
			var rightOffsetParent = Mathf.Tan(Mathf.Deg2Rad * _rightAngle) * parentRect.size.y;
			var leftSizeReduction = Mathf.Tan(Mathf.Deg2Rad * _leftAngle) * (parentRect.size.y - size.y);
			var rightSizeReduction = Mathf.Tan(Mathf.Deg2Rad * _rightAngle) * (parentRect.size.y - size.y);
			var borderSizePixels = borderWidth;
			var xMp = _inverted ? -1 : 1;
			var rightSin = Mathf.Sin((90 - _rightAngle) * Mathf.Deg2Rad) * borderSizePixels;
			var rightCos = Mathf.Cos((90 - _rightAngle) * Mathf.Deg2Rad) * borderSizePixels * xMp;
			var leftSin = Mathf.Sin((90 - _leftAngle) * Mathf.Deg2Rad) * borderSizePixels;
			var leftCos = Mathf.Cos((90 - _leftAngle) * Mathf.Deg2Rad) * borderSizePixels * xMp;
			if (_inverted)
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

			if (!mask)
			{
				_vertexes = new[] {topRight, topLeft, bottomLeft, bottomRight};
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
	}
}