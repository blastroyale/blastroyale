using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Draws a radial progress element (ring). You can set the track color, progress color, and width.
	///
	/// Hint: Make the width half the size of the element to get a full circular loader.
	/// </summary>
	public class RadialProgressElement : VisualElement
	{
		private static readonly CustomStyleProperty<Color> _trackColorStyle = new("--track-color");
		private static readonly CustomStyleProperty<Color> _progressColorStyle = new("--progress-color");
		private static readonly CustomStyleProperty<float> _trackWidthStyle = new("--track-width");

		private float _progress;
		private float _trackWidth = 10f;
		private Color _trackColor = Color.grey;
		private Color _progressColor = Color.green;

		public float Progress
		{
			get => _progress;
			set
			{
				_progress = Mathf.Clamp01(value);
				MarkDirtyRepaint();
			}
		}
		public Color TrackColor
		{
			get => _trackColor;
			set
			{
				_trackColor = value; 
				MarkDirtyRepaint();
			}
		}

		public RadialProgressElement()
		{
			RegisterCallback<CustomStyleResolvedEvent>(CustomStylesResolved);

			generateVisualContent += GenerateVisualContent;
		}

		static void CustomStylesResolved(CustomStyleResolvedEvent evt)
		{
			var element = (RadialProgressElement) evt.currentTarget;
			element.UpdateCustomStyles();
		}

		/// <summary>
		/// Parse the variables from the style, this is useful for resetting to the original state
		/// </summary>
		public void ParseStyles()
		{
			UpdateCustomStyles();
		}
		
		[SuppressMessage("ReSharper", "ConvertIfToOrExpression")]
		[SuppressMessage("ReSharper", "ReplaceWithSingleAssignment.False")]
		private void UpdateCustomStyles()
		{
			var repaint = false;

			if (customStyle.TryGetValue(_progressColorStyle, out _progressColor))
			{
				repaint = true;
			}

			if (customStyle.TryGetValue(_trackColorStyle, out _trackColor))
			{
				repaint = true;
			}

			if (customStyle.TryGetValue(_trackWidthStyle, out _trackWidth))
			{
				repaint = true;
			}

			if (repaint)
			{
				MarkDirtyRepaint();
			}
		}

		private void GenerateVisualContent(MeshGenerationContext context)
		{
			var size = Mathf.Min(contentRect.width, contentRect.height);

			var painter = context.painter2D;
			painter.lineWidth = _trackWidth;
			painter.lineCap = LineCap.Butt;

			// Draw the track
			painter.strokeColor = _trackColor;
			painter.BeginPath();
			painter.Arc(new Vector2(size * 0.5f, size * 0.5f), size * 0.5f, 0.0f, 360.0f);
			painter.Stroke();

			// Draw the progress
			painter.strokeColor = _progressColor;
			painter.BeginPath();
			painter.Arc(new Vector2(size * 0.5f, size * 0.5f), size * 0.5f, -90.0f,
				360.0f * Progress - 90.0f);
			painter.Stroke();
		}

		public new class UxmlFactory : UxmlFactory<RadialProgressElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlFloatAttributeDescription _progressAttribute = new()
			{
				name = "progress"
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				((RadialProgressElement) ve).Progress = _progressAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}