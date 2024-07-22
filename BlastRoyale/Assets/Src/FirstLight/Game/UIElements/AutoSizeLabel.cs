using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// !!EXPERIMENTAL!!
	/// A label that automatically resizes text to fit it's bounds.
	///
	/// IMPORTANT NOTE:
	/// This element allegedly doesn't work with flexGrow as it leads to undefined behaviour (recursion).
	/// Use Size/Width[%] and Size/Height attributes instead
	/// </summary>
	public sealed class AutoSizeLabel : LabelOutlined
	{
		private int minFontSize { get; set; }
		private int maxFontSize { get; set; }

		public override string text
		{
			get => base.text;
			set
			{
				base.text = value;
				UpdateFontSize();
			}
		}

		public AutoSizeLabel() : this(string.Empty)
		{
		}

		public AutoSizeLabel(string text, int minFontSize = 10, int maxFontSize = 200) : base(text)
		{
			this.minFontSize = minFontSize;
			this.maxFontSize = maxFontSize;

			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			UpdateFontSize();
		}

		private void UpdateFontSize()
		{
			var textSize = MeasureTextSize(text,
				float.MaxValue, MeasureMode.AtMost, float.MaxValue, MeasureMode.AtMost);

			// Unity can return a font size of 0 which would break the auto fit
			// Should probably wait till the end of frame to get the real font size
			var fontSize = Mathf.Max(style.fontSize.value.value, 1);
			var heightDictatedFontSize = Mathf.Abs(contentRect.height);
			var widthDictatedFontSize = Mathf.Abs(contentRect.width / textSize.x) * fontSize;
			var newFontSize = (float) Mathf.FloorToInt(Mathf.Min(heightDictatedFontSize, widthDictatedFontSize));
			newFontSize = Mathf.Clamp(newFontSize, minFontSize, maxFontSize);

			if (Mathf.Abs(newFontSize - fontSize) > 1)
			{
				style.fontSize = new StyleLength(new Length(newFontSize));
			}
		}

		public new class UxmlFactory : UxmlFactory<AutoSizeLabel, UxmlTraits>
		{
		}

		public new class UxmlTraits : LabelOutlined.UxmlTraits
		{
			readonly UxmlIntAttributeDescription _minFontSize = new ()
			{
				name = "min-font-size",
				defaultValue = 10,
				restriction = new UxmlValueBounds {min = "1"}
			};

			readonly UxmlIntAttributeDescription _maxFontSize = new ()
			{
				name = "max-font-size",
				defaultValue = 200,
				restriction = new UxmlValueBounds {min = "1"}
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				var instance = (AutoSizeLabel) ve;
				instance.minFontSize = Mathf.Max(_minFontSize.GetValueFromBag(bag, cc), 1);
				instance.maxFontSize = Mathf.Max(_maxFontSize.GetValueFromBag(bag, cc), 1);

				base.Init(ve, bag, cc);
			}
		}
	}
}